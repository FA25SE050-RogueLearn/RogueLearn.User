// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/GenerateQuestSteps/GenerateQuestStepsCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;
using AutoMapper;
using System.Text.Json.Serialization;
using RogueLearn.User.Application.Common;

namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;

/// <summary>
/// Generates weekly learning modules for a quest, with each week containing multiple activities.
/// Activities are sourced from enriched syllabus URLs and validated for skill mapping.
/// </summary>
public class GenerateQuestStepsCommandHandler : IRequestHandler<GenerateQuestStepsCommand, List<GeneratedQuestStepDto>>
{
    private record AiActivity(
        [property: JsonPropertyName("activityId")] string ActivityId,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("payload")] JsonElement Payload
    );

    private readonly IQuestRepository _questRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<GenerateQuestStepsCommandHandler> _logger;
    private readonly IQuestGenerationPlugin _questGenerationPlugin;
    private readonly IMapper _mapper;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IClassRepository _classRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ISubjectSkillMappingRepository _subjectSkillMappingRepository;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IUserSkillRepository _userSkillRepository;

    // ⭐ Configuration for weekly grouping
    private const int SessionsPerWeek = 5; // Group every 5 sessions into 1 week

    public GenerateQuestStepsCommandHandler(
        IQuestRepository questRepository,
        IQuestStepRepository questStepRepository,
        ISubjectRepository subjectRepository,
        ILogger<GenerateQuestStepsCommandHandler> logger,
        IQuestGenerationPlugin questGenerationPlugin,
        IMapper mapper,
        IUserProfileRepository userProfileRepository,
        IClassRepository classRepository,
        ISkillRepository skillRepository,
        ISubjectSkillMappingRepository subjectSkillMappingRepository,
        IPromptBuilder promptBuilder,
        IUserSkillRepository userSkillRepository)
    {
        _questRepository = questRepository;
        _questStepRepository = questStepRepository;
        _subjectRepository = subjectRepository;
        _logger = logger;
        _questGenerationPlugin = questGenerationPlugin;
        _mapper = mapper;
        _userProfileRepository = userProfileRepository;
        _classRepository = classRepository;
        _skillRepository = skillRepository;
        _subjectSkillMappingRepository = subjectSkillMappingRepository;
        _promptBuilder = promptBuilder;
        _userSkillRepository = userSkillRepository;
    }

    public async Task<List<GeneratedQuestStepDto>> Handle(GenerateQuestStepsCommand request, CancellationToken cancellationToken)
    {
        // 1. PRE-CONDITION CHECKS
        var questHasSteps = await _questStepRepository.QuestContainsSteps(request.QuestId, cancellationToken);
        if (questHasSteps)
        {
            throw new BadRequestException("Quest Steps already created.");
        }

        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken)
            ?? throw new NotFoundException("User Profile not found.");

        if (!userProfile.ClassId.HasValue)
        {
            throw new BadRequestException("Please choose a Class first");
        }

        var userClass = await _classRepository.GetByIdAsync(userProfile.ClassId.Value, cancellationToken)
            ?? throw new BadRequestException("Class not found");

        var quest = await _questRepository.GetByIdAsync(request.QuestId, cancellationToken)
            ?? throw new NotFoundException("Quest", request.QuestId);

        if (quest.SubjectId is null)
        {
            throw new BadRequestException("Quest is not associated with a subject.");
        }

        var subject = await _subjectRepository.GetByIdAsync(quest.SubjectId.Value, cancellationToken)
            ?? throw new NotFoundException("Subject", quest.SubjectId.Value);

        if (subject.Content is null || !subject.Content.Any())
        {
            throw new BadRequestException("No syllabus content available for this quest's subject.");
        }

        // 2. VERIFY URL ENRICHMENT
        if (!HasEnrichedUrls(subject.Content))
        {
            _logger.LogWarning(
                "⚠️ Subject {SubjectId} appears to lack URL enrichment. " +
                "Import the syllabus again to populate SuggestedUrl fields.",
                subject.Id);
        }

        // 3. SKILL UNLOCKING & FETCHING
        var skillMappings = await _subjectSkillMappingRepository.GetMappingsBySubjectIdsAsync(
            new[] { quest.SubjectId.Value }, cancellationToken);

        var relevantSkillIds = skillMappings.Select(m => m.SkillId).ToHashSet();

        if (!relevantSkillIds.Any())
        {
            throw new BadRequestException("No skills are linked to this subject. Skill mapping may be incomplete.");
        }

        var relevantSkills = (await _skillRepository.GetAllAsync(cancellationToken))
            .Where(s => relevantSkillIds.Contains(s.Id))
            .ToList();

        // Unlock skills
        var existingUserSkills = await _userSkillRepository.GetSkillsByAuthIdAsync(request.AuthUserId, cancellationToken);
        var existingSkillIds = existingUserSkills.Select(us => us.SkillId).ToHashSet();

        int unlockedCount = 0;
        foreach (var skill in relevantSkills)
        {
            if (!existingSkillIds.Contains(skill.Id))
            {
                var newUserSkill = new UserSkill
                {
                    AuthUserId = request.AuthUserId,
                    SkillId = skill.Id,
                    SkillName = skill.Name,
                    ExperiencePoints = 0,
                    Level = 1
                };
                await _userSkillRepository.AddAsync(newUserSkill, cancellationToken);
                unlockedCount++;
            }
        }

        if (unlockedCount > 0)
        {
            _logger.LogInformation("Unlocked {Count} new skills for User {AuthUserId} upon starting Quest {QuestId}",
                unlockedCount, request.AuthUserId, request.QuestId);
        }

        // 4. PREPARE DATA FOR AI
        var userContext = await _promptBuilder.GenerateAsync(userProfile, userClass, cancellationToken);

        var syllabusJson = Newtonsoft.Json.JsonConvert.SerializeObject(
            subject.Content,
            Newtonsoft.Json.Formatting.Indented,
            new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            }
        );

        // 5. CALL AI TO GENERATE ACTIVITIES
        _logger.LogInformation("Calling AI to generate quest steps for Subject '{SubjectName}'", subject.SubjectName);

        var generatedModuleJson = await _questGenerationPlugin.GenerateQuestStepsJsonAsync(
            syllabusJson, userContext, relevantSkills, subject.SubjectName, subject.Description ?? "", cancellationToken);

        if (string.IsNullOrWhiteSpace(generatedModuleJson))
        {
            throw new InvalidOperationException("AI failed to generate valid activities.");
        }

        // 6. DESERIALIZE AI RESPONSE
        JsonElement activitiesElement;
        try
        {
            var jsonDoc = JsonDocument.Parse(generatedModuleJson);
            if (!jsonDoc.RootElement.TryGetProperty("activities", out activitiesElement) ||
                activitiesElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("AI response is missing the root 'activities' array.");
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON deserialization failed. Raw response: {JsonContent}", generatedModuleJson);
            throw new InvalidOperationException("AI response was not in the correct JSON format.", jsonEx);
        }

        // 7. VALIDATE AND ORGANIZE ACTIVITIES
        var validatedActivities = ValidateActivities(activitiesElement, relevantSkillIds);

        if (!validatedActivities.Any())
        {
            throw new InvalidOperationException("AI failed to generate any valid activities after validation.");
        }

        // 8. ⭐ GROUP ACTIVITIES INTO WEEKLY MODULES
        var weeklyModules = GroupActivitiesIntoWeeks(validatedActivities, subject.SubjectName);

        // 9. PERSIST WEEKLY MODULES AS QUEST STEPS
        var createdSteps = new List<QuestStep>();
        int stepNumber = 1;

        foreach (var weekModule in weeklyModules)
        {
            var questStep = new QuestStep
            {
                QuestId = request.QuestId,
                StepNumber = stepNumber,
                Title = weekModule.Title,
                Description = weekModule.Description,
                ExperiencePoints = CalculateTotalExperience(weekModule.Activities),
                Content = new Dictionary<string, object> { { "activities", weekModule.Activities } }
            };

            await _questStepRepository.AddAsync(questStep, cancellationToken);
            createdSteps.Add(questStep);

            _logger.LogInformation(
                "✅ Created Week {WeekNumber} with {ActivityCount} activities ({XP} XP)",
                stepNumber, weekModule.Activities.Count, questStep.ExperiencePoints);

            stepNumber++;
        }

        _logger.LogInformation(
            "✅ Successfully generated {WeekCount} weekly modules with {TotalActivities} total activities for Quest {QuestId}",
            weeklyModules.Count, validatedActivities.Count, request.QuestId);

        return _mapper.Map<List<GeneratedQuestStepDto>>(createdSteps);
    }

    /// <summary>
    /// Validates activities and regenerates invalid activity IDs.
    /// </summary>
    private List<Dictionary<string, object>> ValidateActivities(
        JsonElement activitiesElement,
        HashSet<Guid> relevantSkillIds)
    {
        var validatedActivities = new List<Dictionary<string, object>>();
        int discardedCount = 0;
        int readingsWithoutUrls = 0;
        int invalidActivityIds = 0;

        foreach (var activityElement in activitiesElement.EnumerateArray())
        {
            var activity = JsonSerializer.Deserialize<AiActivity>(
                activityElement.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (activity == null)
            {
                discardedCount++;
                continue;
            }

            // Validate and potentially regenerate activityId
            string finalActivityId = activity.ActivityId;
            if (string.IsNullOrWhiteSpace(activity.ActivityId) || !Guid.TryParse(activity.ActivityId, out _))
            {
                finalActivityId = Guid.NewGuid().ToString();
                invalidActivityIds++;
                _logger.LogWarning(
                    "Activity had invalid activityId '{OldId}'. Generated new ID: {NewId}",
                    activity.ActivityId ?? "null",
                    finalActivityId);
            }

            if (activity.Payload.ValueKind != JsonValueKind.Object)
            {
                _logger.LogWarning("Activity {ActivityId} has malformed payload. Discarding.", finalActivityId);
                discardedCount++;
                continue;
            }

            // Validate skillId
            if (!activity.Payload.TryGetProperty("skillId", out var skillIdElement) ||
                !Guid.TryParse(skillIdElement.GetString(), out var skillId) ||
                !relevantSkillIds.Contains(skillId))
            {
                _logger.LogWarning("Activity {ActivityId} has invalid skillId. Discarding.", finalActivityId);
                discardedCount++;
                continue;
            }

            // For Reading activities, check if URL exists
            if (activity.Type.Equals("Reading", StringComparison.OrdinalIgnoreCase))
            {
                if (activity.Payload.TryGetProperty("url", out var urlElement))
                {
                    var url = urlElement.GetString();
                    if (string.IsNullOrWhiteSpace(url))
                    {
                        readingsWithoutUrls++;
                        _logger.LogWarning("Reading activity '{ActivityId}' has empty URL", finalActivityId);
                    }
                }
                else
                {
                    readingsWithoutUrls++;
                    _logger.LogWarning("Reading activity '{ActivityId}' missing URL property", finalActivityId);
                }
            }

            // Convert to dictionary with corrected activityId
            var activityDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                activityElement.GetRawText(),
                new JsonSerializerOptions
                {
                    Converters = { new ObjectToInferredTypesConverter() }
                });

            if (activityDict != null)
            {
                activityDict["activityId"] = finalActivityId;
                validatedActivities.Add(activityDict);
            }
        }

        if (invalidActivityIds > 0)
        {
            _logger.LogWarning("Regenerated {Count} invalid activity IDs with new GUIDs", invalidActivityIds);
        }

        if (discardedCount > 0)
        {
            _logger.LogWarning("Discarded {Count} activities due to validation failures", discardedCount);
        }

        if (readingsWithoutUrls > 0)
        {
            _logger.LogWarning("⚠️ {Count} Reading activities have missing/empty URLs", readingsWithoutUrls);
        }

        return validatedActivities;
    }

    /// <summary>
    /// ⭐ NEW: Groups activities into weekly modules based on Reading activities (which map to syllabus sessions).
    /// Each week gets ~5 sessions worth of content.
    /// </summary>
    private List<WeeklyModule> GroupActivitiesIntoWeeks(
        List<Dictionary<string, object>> activities,
        string subjectName)
    {
        var weeklyModules = new List<WeeklyModule>();
        var currentWeekActivities = new List<Dictionary<string, object>>();
        int readingCount = 0;
        int weekNumber = 1;

        foreach (var activity in activities)
        {
            currentWeekActivities.Add(activity);

            // Count Reading activities (they represent syllabus sessions)
            if (activity.TryGetValue("type", out var typeObj) &&
                typeObj?.ToString()?.Equals("Reading", StringComparison.OrdinalIgnoreCase) == true)
            {
                readingCount++;
            }

            // When we've accumulated enough sessions, create a new week
            if (readingCount >= SessionsPerWeek)
            {
                var weekModule = new WeeklyModule
                {
                    WeekNumber = weekNumber,
                    Title = GenerateWeekTitle(weekNumber, subjectName, currentWeekActivities),
                    Description = $"Week {weekNumber} learning module covering key concepts in {subjectName}.",
                    Activities = new List<Dictionary<string, object>>(currentWeekActivities)
                };

                weeklyModules.Add(weekModule);

                // Reset for next week
                currentWeekActivities.Clear();
                readingCount = 0;
                weekNumber++;
            }
        }

        // Add remaining activities as final week (if any)
        if (currentWeekActivities.Any())
        {
            var weekModule = new WeeklyModule
            {
                WeekNumber = weekNumber,
                Title = GenerateWeekTitle(weekNumber, subjectName, currentWeekActivities),
                Description = $"Week {weekNumber} learning module covering key concepts in {subjectName}.",
                Activities = new List<Dictionary<string, object>>(currentWeekActivities)
            };

            weeklyModules.Add(weekModule);
        }

        return weeklyModules;
    }

    /// <summary>
    /// Generates a descriptive title for a week based on its activities.
    /// </summary>
    private string GenerateWeekTitle(int weekNumber, string subjectName, List<Dictionary<string, object>> activities)
    {
        // Try to extract the first Reading activity's title for context
        foreach (var activity in activities.Take(2)) // Check first 2 activities
        {
            if (activity.TryGetValue("type", out var typeObj) &&
                typeObj?.ToString()?.Equals("Reading", StringComparison.OrdinalIgnoreCase) == true &&
                activity.TryGetValue("payload", out var payloadObj) &&
                payloadObj is Dictionary<string, object> payload &&
                payload.TryGetValue("articleTitle", out var titleObj))
            {
                var articleTitle = titleObj?.ToString();
                if (!string.IsNullOrWhiteSpace(articleTitle))
                {
                    // Use first few words of article title
                    var titleWords = articleTitle.Split(' ').Take(4);
                    return $"Week {weekNumber}: {string.Join(" ", titleWords)}";
                }
            }
        }

        // Fallback
        return $"Week {weekNumber}: {subjectName}";
    }

    /// <summary>
    /// Checks if the syllabus content has enriched URLs.
    /// </summary>
    private bool HasEnrichedUrls(Dictionary<string, object> content)
    {
        try
        {
            if (content.TryGetValue("sessionSchedule", out var scheduleObj) && scheduleObj is List<object> sessions)
            {
                foreach (var session in sessions.Take(3))
                {
                    if (session is Dictionary<string, object> dict &&
                        dict.TryGetValue("suggestedUrl", out var urlObj) &&
                        !string.IsNullOrWhiteSpace(urlObj?.ToString()))
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return false;
    }

    private int CalculateTotalExperience(List<Dictionary<string, object>> activities)
    {
        int totalXp = 0;
        foreach (var activity in activities)
        {
            if (activity.TryGetValue("payload", out var payloadObj) &&
                payloadObj is Dictionary<string, object> payloadDict &&
                payloadDict.TryGetValue("experiencePoints", out var xpObj) &&
                int.TryParse(xpObj.ToString(), out var xpVal))
            {
                totalXp += xpVal;
            }
        }
        return totalXp;
    }

    /// <summary>
    /// Represents a weekly learning module.
    /// </summary>
    private class WeeklyModule
    {
        public int WeekNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<Dictionary<string, object>> Activities { get; set; } = new();
    }
}
