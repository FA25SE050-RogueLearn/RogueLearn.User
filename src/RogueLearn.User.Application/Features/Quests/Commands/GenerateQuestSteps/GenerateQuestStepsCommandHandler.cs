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
/// Generates weekly learning modules for a quest.
/// Each call to this handler generates ONE week's activities at a time.
/// Supports 5-12 total weeks based on syllabus size.
/// </summary>
public class GenerateQuestStepsCommandHandler : IRequestHandler<GenerateQuestStepsCommand, List<GeneratedQuestStepDto>>
{
    // ========== CONFIGURATION CONSTANTS ==========
    private const int SessionsPerWeek = 5;
    private const int MinQuestSteps = 5;
    private const int MaxQuestSteps = 12;
    private const int MinActivitiesPerStep = 6;
    private const int MaxActivitiesPerStep = 10;
    private const int MinXpPerStep = 250;
    private const int MaxXpPerStep = 400;

    private record AiActivity(
        [property: JsonPropertyName("activityId")] string ActivityId,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("payload")] JsonElement Payload
    );

    private record AiActivitiesResponse(
        [property: JsonPropertyName("activities")] JsonElement Activities
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
    private readonly QuestStepsPromptBuilder _questStepsPromptBuilder;

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
        _questStepsPromptBuilder = new QuestStepsPromptBuilder();
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

        // 5. CALCULATE WEEKS TO GENERATE
        int totalSessions = ExtractTotalSessions(subject.Content);
        int sessionsToUse = totalSessions;

        // Filter sessions if > 100 (keep only important ones)
        if (totalSessions > 100)
        {
            _logger.LogInformation("Filtering {Total} sessions to keep only important ones (~60 target)", totalSessions);
            sessionsToUse = 60; // Target ~60 after filtering (handled by syllabus import ideally)
        }

        int weeksToGenerate = (int)Math.Ceiling((decimal)sessionsToUse / SessionsPerWeek);

        // Validate week count
        if (weeksToGenerate < MinQuestSteps)
        {
            throw new BadRequestException(
                $"Insufficient sessions ({sessionsToUse}). Need at least {MinQuestSteps * SessionsPerWeek} sessions " +
                $"to generate {MinQuestSteps} weeks. Got {weeksToGenerate} weeks.");
        }

        if (weeksToGenerate > MaxQuestSteps)
        {
            _logger.LogWarning(
                "⚠️ Subject has {Sessions} sessions, which would generate {Weeks} weeks. " +
                "Capping at maximum {Max} weeks. Oldest weeks will be excluded.",
                sessionsToUse, weeksToGenerate, MaxQuestSteps);
            weeksToGenerate = MaxQuestSteps;
        }

        _logger.LogInformation(
            "📋 Generation Plan: {Sessions} sessions → {Weeks} weeks (5 sessions/week) → {Weeks} quest steps",
            sessionsToUse, weeksToGenerate, weeksToGenerate);

        // 6. LOOP: GENERATE EACH WEEK
        var createdSteps = new List<QuestStep>();
        int totalActivitiesGenerated = 0;
        int skippedWeeks = 0;

        for (int weekNumber = 1; weekNumber <= weeksToGenerate; weekNumber++)
        {
            try
            {
                _logger.LogInformation("🔄 Generating Week {Week}/{Total}...", weekNumber, weeksToGenerate);

                // Build prompt for this specific week
                string prompt = _questStepsPromptBuilder.BuildPrompt(
                    syllabusJson,
                    userContext,
                    relevantSkills,
                    subject.SubjectName,
                    subject.Description ?? "",
                    weekNumber,
                    weeksToGenerate);

                // Call AI for this week
                var generatedJson = await _questGenerationPlugin.GenerateQuestStepsJsonAsync(
     syllabusJson,
     userContext,
     relevantSkills,
     subject.SubjectName,
     subject.Description ?? "",
     weekNumber,            // ✅ Added
     weeksToGenerate,       // ✅ Added
     cancellationToken);


                if (string.IsNullOrWhiteSpace(generatedJson))
                {
                    _logger.LogError("Week {Week}: AI returned empty response", weekNumber);
                    skippedWeeks++;
                    continue;
                }

                // 7. DESERIALIZE ACTIVITIES ARRAY
                JsonElement activitiesElement;
                try
                {
                    var jsonDoc = JsonDocument.Parse(generatedJson);
                    if (!jsonDoc.RootElement.TryGetProperty("activities", out activitiesElement) ||
                        activitiesElement.ValueKind != JsonValueKind.Array)
                    {
                        _logger.LogError("Week {Week}: Response missing 'activities' array. Response: {Json}",
                            weekNumber, generatedJson);
                        skippedWeeks++;
                        continue;
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Week {Week}: JSON deserialization failed. Raw: {Json}",
                        weekNumber, generatedJson);
                    skippedWeeks++;
                    continue;
                }

                // 8. VALIDATE ACTIVITIES FOR THIS WEEK
                var validatedActivities = ValidateActivities(activitiesElement, relevantSkillIds, weekNumber);

                // Check minimum activities
                if (validatedActivities.Count < MinActivitiesPerStep)
                {
                    _logger.LogWarning(
                        "Week {Week}: Only {Count} activities (minimum {Min}). Skipping.",
                        weekNumber, validatedActivities.Count, MinActivitiesPerStep);
                    skippedWeeks++;
                    continue;
                }

                // Check maximum activities
                if (validatedActivities.Count > MaxActivitiesPerStep)
                {
                    _logger.LogWarning(
                        "Week {Week}: {Count} activities exceeds max {Max}. Trimming.",
                        weekNumber, validatedActivities.Count, MaxActivitiesPerStep);
                    validatedActivities = validatedActivities.Take(MaxActivitiesPerStep).ToList();
                }

                // 9. ANALYZE & LOG WEEK STATISTICS
                var activityStats = AnalyzeActivities(validatedActivities);
                LogWeekStatistics(weekNumber, activityStats);

                // Validate week requirements
                ValidateWeekRequirements(weekNumber, activityStats, subject.SubjectName);

                // Check XP range
                int totalXp = CalculateTotalExperience(validatedActivities);
                if (totalXp < MinXpPerStep || totalXp > MaxXpPerStep)
                {
                    _logger.LogWarning(
                        "Week {Week}: XP is {XP} (target {Min}-{Max}). Acceptable but monitor.",
                        weekNumber, totalXp, MinXpPerStep, MaxXpPerStep);
                }

                // 10. CREATE QUEST STEP
                var questStep = new QuestStep
                {
                    QuestId = request.QuestId,
                    StepNumber = weekNumber,
                    Title = $"Week {weekNumber}: Session {(weekNumber - 1) * SessionsPerWeek + 1}-{weekNumber * SessionsPerWeek}",
                    Description = $"Learning activities for Week {weekNumber}",
                    ExperiencePoints = totalXp,
                    Content = new Dictionary<string, object> { { "activities", validatedActivities } }
                };

                await _questStepRepository.AddAsync(questStep, cancellationToken);
                createdSteps.Add(questStep);
                totalActivitiesGenerated += validatedActivities.Count;

                _logger.LogInformation(
                    "✅ Week {Week}/{Total}: Created with {Activities} activities = {XP} XP",
                    weekNumber, weeksToGenerate, validatedActivities.Count, totalXp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Week {Week}: Unexpected error. Skipping.", weekNumber);
                skippedWeeks++;
            }
        }

        // 11. FINAL VALIDATION
        if (!createdSteps.Any())
        {
            throw new InvalidOperationException(
                $"Failed to generate any valid quest steps. All {weeksToGenerate} weeks were skipped.");
        }

        if (createdSteps.Count < MinQuestSteps)
        {
            _logger.LogWarning(
                "⚠️ Generated {Count} steps (minimum recommended {Min}). " +
                "Consider improving the syllabus content quality.",
                createdSteps.Count, MinQuestSteps);
        }

        _logger.LogInformation(
            "✅ Successfully generated {Steps} quest steps with {Total} total activities " +
            "({Skipped} weeks skipped due to validation failures)",
            createdSteps.Count, totalActivitiesGenerated, skippedWeeks);

        return _mapper.Map<List<GeneratedQuestStepDto>>(createdSteps);
    }

    /// <summary>
    /// Extracts total session count from syllabus content.
    /// </summary>
    private int ExtractTotalSessions(Dictionary<string, object> content)
    {
        try
        {
            if (content.TryGetValue("sessionSchedule", out var scheduleObj) && scheduleObj is List<object> sessions)
            {
                return sessions.Count;
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return 60; // Default assumption
    }

    /// <summary>
    /// Validates activities and regenerates invalid activity IDs.
    /// </summary>
    private List<Dictionary<string, object>> ValidateActivities(
        JsonElement activitiesElement,
        HashSet<Guid> relevantSkillIds,
        int weekNumber)
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
                    "Week {Week}: Activity had invalid activityId '{Old}'. Generated: {New}",
                    weekNumber, activity.ActivityId ?? "null", finalActivityId);
            }

            if (activity.Payload.ValueKind != JsonValueKind.Object)
            {
                _logger.LogWarning("Week {Week}: Activity {Id} has malformed payload. Discarding.",
                    weekNumber, finalActivityId);
                discardedCount++;
                continue;
            }

            // Validate skillId
            if (!activity.Payload.TryGetProperty("skillId", out var skillIdElement) ||
                !Guid.TryParse(skillIdElement.GetString(), out var skillId) ||
                !relevantSkillIds.Contains(skillId))
            {
                _logger.LogWarning("Week {Week}: Activity {Id} has invalid skillId. Discarding.",
                    weekNumber, finalActivityId);
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
                        _logger.LogWarning("Week {Week}: Reading {Id} has empty URL",
                            weekNumber, finalActivityId);
                    }
                }
                else
                {
                    readingsWithoutUrls++;
                    _logger.LogWarning("Week {Week}: Reading {Id} missing URL property",
                        weekNumber, finalActivityId);
                }
            }

            // For KnowledgeCheck, validate it has multiple questions (3-5)
            if (activity.Type.Equals("KnowledgeCheck", StringComparison.OrdinalIgnoreCase))
            {
                if (activity.Payload.TryGetProperty("questions", out var questionsElement) &&
                    questionsElement.ValueKind == JsonValueKind.Array)
                {
                    int questionCount = questionsElement.GetArrayLength();
                    if (questionCount < 3 || questionCount > 5)
                    {
                        _logger.LogWarning(
                            "Week {Week}: KnowledgeCheck has {Count} questions (expected 3-5)",
                            weekNumber, questionCount);
                    }
                }
                else
                {
                    _logger.LogWarning("Week {Week}: KnowledgeCheck missing 'questions' array", weekNumber);
                    discardedCount++;
                    continue;
                }
            }

            // For Quiz, validate it has 10-15 questions
            if (activity.Type.Equals("Quiz", StringComparison.OrdinalIgnoreCase))
            {
                if (activity.Payload.TryGetProperty("questions", out var quizQuestionsElement) &&
                    quizQuestionsElement.ValueKind == JsonValueKind.Array)
                {
                    int questionCount = quizQuestionsElement.GetArrayLength();
                    if (questionCount < 10 || questionCount > 15)
                    {
                        _logger.LogWarning(
                            "Week {Week}: Quiz has {Count} questions (expected 10-15)",
                            weekNumber, questionCount);
                    }
                }
                else
                {
                    _logger.LogWarning("Week {Week}: Quiz missing 'questions' array", weekNumber);
                    discardedCount++;
                    continue;
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
            _logger.LogWarning("Week {Week}: Regenerated {Count} invalid activity IDs", weekNumber, invalidActivityIds);
        }

        if (discardedCount > 0)
        {
            _logger.LogWarning("Week {Week}: Discarded {Count} activities", weekNumber, discardedCount);
        }

        if (readingsWithoutUrls > 0)
        {
            _logger.LogWarning("Week {Week}: ⚠️ {Count} Reading activities have missing/empty URLs", weekNumber, readingsWithoutUrls);
        }

        return validatedActivities;
    }

    /// <summary>
    /// Analyzes activity distribution in a week.
    /// </summary>
    private ActivityStats AnalyzeActivities(List<Dictionary<string, object>> activities)
    {
        var stats = new ActivityStats();

        foreach (var activity in activities)
        {
            if (!activity.TryGetValue("type", out var typeObj))
                continue;

            var type = typeObj?.ToString();
            switch (type?.ToLowerInvariant())
            {
                case "reading":
                    stats.ReadingCount++;
                    break;
                case "knowledgecheck":
                    stats.KnowledgeCheckCount++;
                    if (activity.TryGetValue("payload", out var kcPayload) &&
                        kcPayload is Dictionary<string, object> kcDict &&
                        kcDict.TryGetValue("questions", out var kcQuestions) &&
                        kcQuestions is JsonElement kcQuestionsElement &&
                        kcQuestionsElement.ValueKind == JsonValueKind.Array)
                    {
                        stats.TotalKnowledgeCheckQuestions += kcQuestionsElement.GetArrayLength();
                    }
                    break;
                case "quiz":
                    stats.QuizCount++;
                    if (activity.TryGetValue("payload", out var quizPayload) &&
                        quizPayload is Dictionary<string, object> quizDict &&
                        quizDict.TryGetValue("questions", out var quizQuestions) &&
                        quizQuestions is JsonElement quizQuestionsElement &&
                        quizQuestionsElement.ValueKind == JsonValueKind.Array)
                    {
                        stats.TotalQuizQuestions += quizQuestionsElement.GetArrayLength();
                    }
                    break;
                case "coding":
                    stats.CodingCount++;
                    break;
            }
        }

        stats.TotalActivities = activities.Count;
        return stats;
    }

    /// <summary>
    /// Logs statistics for a week.
    /// </summary>
    private void LogWeekStatistics(int weekNumber, ActivityStats stats)
    {
        _logger.LogInformation(
            "Week {Week} Stats: {Total}A ({R}R, {KC}KC[{KCQ}Q], {Q}Quiz[{QQ}Q], {C}C)",
            weekNumber,
            stats.TotalActivities,
            stats.ReadingCount,
            stats.KnowledgeCheckCount,
            stats.TotalKnowledgeCheckQuestions,
            stats.QuizCount,
            stats.TotalQuizQuestions,
            stats.CodingCount);
    }

    /// <summary>
    /// Validates week meets minimum requirements.
    /// </summary>
    private void ValidateWeekRequirements(int weekNumber, ActivityStats stats, string subjectName)
    {
        if (stats.TotalActivities < MinActivitiesPerStep)
        {
            _logger.LogWarning("⚠️ Week {Week}: Only {Count} activities (min {Min})",
                weekNumber, stats.TotalActivities, MinActivitiesPerStep);
        }

        if (stats.ReadingCount < 1)
        {
            _logger.LogWarning("⚠️ Week {Week}: NO Reading activities!", weekNumber);
        }

        if (stats.QuizCount < 1)
        {
            _logger.LogWarning("⚠️ Week {Week}: NO Quiz!", weekNumber);
        }

        if (stats.QuizCount > 0 && stats.TotalQuizQuestions < 10)
        {
            _logger.LogWarning("⚠️ Week {Week}: Quiz has {Count} questions (min 10)",
                weekNumber, stats.TotalQuizQuestions);
        }

        var isTechnical = IsTechnicalSubject(subjectName);
        if (isTechnical && stats.CodingCount < 1)
        {
            _logger.LogWarning("⚠️ Week {Week}: Technical subject missing Coding challenge!",
                weekNumber);
        }
    }

    /// <summary>
    /// Determines if a subject is technical.
    /// </summary>
    private bool IsTechnicalSubject(string subjectName)
    {
        var nameLower = subjectName.ToLowerInvariant();
        return nameLower.Contains("programming") ||
               nameLower.Contains("mobile") ||
               nameLower.Contains("web") ||
               nameLower.Contains("software") ||
               nameLower.Contains("development") ||
               nameLower.Contains("coding") ||
               nameLower.Contains("android") ||
               nameLower.Contains("java") ||
               nameLower.Contains("c#") ||
               nameLower.Contains(".net");
    }

    /// <summary>
    /// Checks if syllabus has enriched URLs.
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
        catch { }

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

    private class ActivityStats
    {
        public int TotalActivities { get; set; }
        public int ReadingCount { get; set; }
        public int KnowledgeCheckCount { get; set; }
        public int TotalKnowledgeCheckQuestions { get; set; }
        public int QuizCount { get; set; }
        public int TotalQuizQuestions { get; set; }
        public int CodingCount { get; set; }
    }
}
