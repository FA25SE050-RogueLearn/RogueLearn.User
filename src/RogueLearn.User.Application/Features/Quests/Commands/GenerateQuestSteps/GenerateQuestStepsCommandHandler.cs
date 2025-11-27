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
using Hangfire;
using System.Text.RegularExpressions;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;

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
        // ========== 1. PRE-CONDITION CHECKS ==========
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

        // ========== 2. PREPARE SYLLABUS SESSIONS ==========
        // Serialize full content with Newtonsoft to preserve JToken/JArray structures, then parse with STJ
        List<SyllabusSessionDto> allSessions = new();
        try
        {
            var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(subject.Content);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            options.Converters.Add(new SyllabusSessionDtoConverter());

            var content = JsonSerializer.Deserialize<SyllabusContent>(jsonString, options) ?? new SyllabusContent();
            content.SessionSchedule ??= new List<SyllabusSessionDto>();
            allSessions = content.SessionSchedule;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize syllabus content for Subject {Subject}", subject.SubjectName);
        }

        if (!allSessions.Any())
        {
            throw new BadRequestException("Syllabus content is missing 'SessionSchedule' or it could not be parsed. Import the syllabus again.");
        }

        _logger.LogInformation("Extracted {Count} sessions from syllabus for Subject {Subject}", allSessions.Count, subject.SubjectName);

        // ========== 3. SKILL UNLOCKING ==========
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

        // Unlock skills (Level 0)
        var existingUserSkills = await _userSkillRepository.GetSkillsByAuthIdAsync(request.AuthUserId, cancellationToken);
        var existingSkillIds = existingUserSkills.Select(us => us.SkillId).ToHashSet();
        var newUserSkills = relevantSkills
            .Where(skill => !existingSkillIds.Contains(skill.Id))
            .Select(skill => new UserSkill
            {
                AuthUserId = request.AuthUserId,
                SkillId = skill.Id,
                SkillName = skill.Name,
                ExperiencePoints = 0,
                Level = 0 // Unlocked/Discovered
            })
            .ToList();

        if (newUserSkills.Any())
        {
            await _userSkillRepository.AddRangeAsync(newUserSkills, cancellationToken);
        }

        // ========== 4. CALCULATE WEEK PLAN ==========
        int totalSessions = allSessions.Count;
        int weeksToGenerate = (int)Math.Ceiling((decimal)totalSessions / SessionsPerWeek);

        // Cap max weeks if syllabus is overly long
        if (weeksToGenerate > MaxQuestSteps) weeksToGenerate = MaxQuestSteps;
        if (weeksToGenerate < MinQuestSteps) weeksToGenerate = MinQuestSteps; // Ensure minimum experience

        UpdateHangfireJobProgress(request.HangfireJobId, 0, weeksToGenerate, "Starting quest generation...");

        var userContextString = await _promptBuilder.GenerateAsync(userProfile, userClass, cancellationToken);

        var createdSteps = new List<QuestStep>();
        int skippedWeeks = 0;

        // ========== 5. GENERATE EACH WEEK ==========
        for (int weekNumber = 1; weekNumber <= weeksToGenerate; weekNumber++)
        {
            try
            {
                UpdateHangfireJobProgress(request.HangfireJobId, weekNumber - 1, weeksToGenerate, $"Generating week {weekNumber}...");

                // --- RESOURCE POOLING & AGGREGATION LOGIC ---

                // Identify sessions for this week
                // Calculate start/end indices safe against out-of-bounds
                int startIndex = (weekNumber - 1) * SessionsPerWeek;
                // If we run out of sessions but still need to generate weeks (to meet MinQuestSteps), 
                // reuse the last few sessions or just the available ones.
                if (startIndex >= totalSessions) startIndex = Math.Max(0, totalSessions - SessionsPerWeek);

                var weekSessions = allSessions.Skip(startIndex).Take(SessionsPerWeek).ToList();

                // If weekSessions is empty (shouldn't happen with logic above), take *some* sessions
                if (!weekSessions.Any()) weekSessions = allSessions.TakeLast(5).ToList();

                // Build WeekContext
                var weekContext = new WeekContext
                {
                    WeekNumber = weekNumber,
                    TotalWeeks = weeksToGenerate,
                    TopicsToCover = weekSessions
                        .Where(s => !string.IsNullOrWhiteSpace(s.Topic))
                        .Select(s => s.Topic)
                        .Distinct()
                        .ToList(),
                    AvailableResources = weekSessions
                        .Where(s => !string.IsNullOrWhiteSpace(s.SuggestedUrl))
                        .Select(s => new ValidResource
                        {
                            Url = CleanUrl(s.SuggestedUrl!),
                            SourceContext = s.Topic
                        })
                        .GroupBy(r => r.Url) // Deduplicate by URL
                        .Select(g => g.First())
                        .ToList()
                };

                _logger.LogInformation("Week {Week}: Pooled {UrlCount} URLs for {TopicCount} topics.",
                    weekNumber, weekContext.AvailableResources.Count, weekContext.TopicsToCover.Count);
                if (weekContext.TopicsToCover.Count == 0 || weekContext.AvailableResources.Count == 0)
                {
                    var sampleTopics = string.Join(" | ", weekSessions.Select(s => s.Topic).Take(3));
                    var sampleUrls = string.Join(" | ", weekSessions.Select(s => s.SuggestedUrl).Take(3));
                    _logger.LogWarning("Week {Week}: Empty pools. Sample topics: {Topics}. Sample urls: {Urls}",
                        weekNumber, sampleTopics, sampleUrls);
                }

                // Call Plugin with WeekContext
                var generatedJson = await _questGenerationPlugin.GenerateQuestStepsJsonAsync(
                    weekContext,
                    userContextString,
                    relevantSkills,
                    subject.SubjectName,
                    subject.Description ?? "",
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(generatedJson))
                {
                    _logger.LogError("Week {Week}: Empty AI response. Skipping.", weekNumber);
                    skippedWeeks++;
                    continue;
                }

                // Parse & Validate
                JsonElement activitiesElement;
                using var doc = JsonDocument.Parse(generatedJson);
                if (!doc.RootElement.TryGetProperty("activities", out activitiesElement))
                {
                    _logger.LogError("Week {Week}: Missing 'activities' property.", weekNumber);
                    skippedWeeks++;
                    continue;
                }

                var validatedActivities = ValidateActivities(activitiesElement, relevantSkillIds, weekNumber);

                // Ensure Math notation cleanup
                validatedActivities = ProcessMathInActivities(validatedActivities);

                // Enforce Constraints (Post-Generation Quality Control)
                var stats = AnalyzeActivities(validatedActivities);
                LogWeekStatistics(weekNumber, stats);
                ValidateWeekRequirements(weekNumber, stats, subject.SubjectName);

                if (validatedActivities.Count < MinActivitiesPerStep)
                {
                    _logger.LogWarning("Week {Week}: Too few activities ({Count}). Skipping.", weekNumber, validatedActivities.Count);
                    skippedWeeks++;
                    continue;
                }
                if (validatedActivities.Count > MaxActivitiesPerStep)
                {
                    validatedActivities = validatedActivities.Take(MaxActivitiesPerStep).ToList();
                }

                // Calculate XP
                int totalXp = CalculateTotalExperience(validatedActivities);

                // Create Step
                var questStep = new QuestStep
                {
                    QuestId = request.QuestId,
                    StepNumber = weekNumber,
                    Title = $"Week {weekNumber}: {SummarizeTopics(weekContext.TopicsToCover)}",
                    Description = $"Mastering concepts from Week {weekNumber}",
                    ExperiencePoints = totalXp,
                    Content = new Dictionary<string, object> { { "activities", validatedActivities } }
                };

                await _questStepRepository.AddAsync(questStep, cancellationToken);
                createdSteps.Add(questStep);

                _logger.LogInformation("✅ Week {Week} created with {Count} activities.", weekNumber, validatedActivities.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generating Week {Week}", weekNumber);
                skippedWeeks++;
            }
        }

        UpdateHangfireJobProgress(request.HangfireJobId, weeksToGenerate, weeksToGenerate, "Completed");

        if (!createdSteps.Any()) throw new InvalidOperationException("Failed to generate any quest steps.");

        return _mapper.Map<List<GeneratedQuestStepDto>>(createdSteps);
    }

    // ========== HELPERS ==========

    private string SummarizeTopics(List<string> topics)
    {
        if (!topics.Any()) return "General Concepts";
        var first = topics.First().Split('.')[0].Split(':')[0];
        if (topics.Count > 1) return $"{first} and more";
        return first.Length > 50 ? first.Substring(0, 47) + "..." : first;
    }

    private void UpdateHangfireJobProgress(string jobId, int current, int total, string message)
    {
        if (string.IsNullOrEmpty(jobId)) return;
        try
        {
            var progressData = new
            {
                CurrentStep = current,
                TotalSteps = total,
                Message = message,
                ProgressPercentage = (int)Math.Round((decimal)current / total * 100),
                UpdatedAt = DateTime.UtcNow
            };
            JobStorage.Current.GetConnection().SetJobParameter(jobId, "Progress", JsonSerializer.Serialize(progressData));
        }
        catch { /* Best effort */ }
    }

    private List<Dictionary<string, object>> ValidateActivities(JsonElement activitiesElement, HashSet<Guid> relevantSkillIds, int weekNumber)
    {
        var validated = new List<Dictionary<string, object>>();
        foreach (var el in activitiesElement.EnumerateArray())
        {
            // Deserialize to dictionary for manipulation
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText(), new JsonSerializerOptions { Converters = { new ObjectToInferredTypesConverter() } });

            if (dict == null) continue;

            // Ensure ID
            if (!dict.ContainsKey("activityId") || !Guid.TryParse(dict["activityId"]?.ToString(), out _))
            {
                dict["activityId"] = Guid.NewGuid().ToString();
            }

            // Validation Logic (Skill Check)
            if (dict.TryGetValue("payload", out var payloadObj) && payloadObj is Dictionary<string, object> payload)
            {
                if (payload.TryGetValue("skillId", out var sIdObj) && Guid.TryParse(sIdObj.ToString(), out var sId))
                {
                    // If skill is invalid, map to a random valid one to save the activity
                    if (!relevantSkillIds.Contains(sId) && relevantSkillIds.Any())
                    {
                        payload["skillId"] = relevantSkillIds.First().ToString();
                    }
                }
            }
            validated.Add(dict);
        }
        return validated;
    }

    private List<Dictionary<string, object>> ProcessMathInActivities(List<Dictionary<string, object>> activities)
    {
        var result = new List<Dictionary<string, object>>();
        foreach (var activity in activities)
        {
            if (!activity.TryGetValue("type", out var typeObj) || typeObj is null)
            {
                result.Add(activity);
                continue;
            }

            var type = typeObj.ToString()?.ToLowerInvariant();
            if (!activity.TryGetValue("payload", out var payloadObj) || payloadObj is not Dictionary<string, object> payload)
            {
                result.Add(activity);
                continue;
            }

            if (type == "reading")
            {
                if (payload.TryGetValue("articleTitle", out var atObj) && atObj is string atStr)
                {
                    payload["articleTitle"] = NormalizePlainTextMath(atStr);
                }
                if (payload.TryGetValue("summary", out var sumObj) && sumObj is string sumStr)
                {
                    payload["summary"] = NormalizePlainTextMath(sumStr);
                }
            }

            if (type == "knowledgecheck" || type == "quiz")
            {
                if (payload.TryGetValue("questions", out var questionsObj))
                {
                    var newQuestions = new List<object>();
                    if (questionsObj is JsonElement qe && qe.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var qEl in qe.EnumerateArray())
                        {
                            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(qEl.GetRawText(), new JsonSerializerOptions { Converters = { new ObjectToInferredTypesConverter() } });
                            if (dict is not null)
                            {
                                newQuestions.Add(NormalizeQuestionDict(dict));
                            }
                        }
                    }
                    else if (questionsObj is List<object> qList)
                    {
                        foreach (var q in qList)
                        {
                            if (q is Dictionary<string, object> qDict)
                            {
                                newQuestions.Add(NormalizeQuestionDict(qDict));
                            }
                            else if (q is JsonElement qEl && qEl.ValueKind == JsonValueKind.Object)
                            {
                                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(qEl.GetRawText(), new JsonSerializerOptions { Converters = { new ObjectToInferredTypesConverter() } });
                                if (dict is not null)
                                {
                                    newQuestions.Add(NormalizeQuestionDict(dict));
                                }
                            }
                            else
                            {
                                newQuestions.Add(q);
                            }
                        }
                    }
                    else
                    {
                        newQuestions.Add(questionsObj);
                    }
                    payload["questions"] = newQuestions;
                }
            }

            result.Add(activity);
        }

        return result;
    }

    private static string CleanUrl(string url)
    {
        return (url ?? string.Empty).Replace("`", string.Empty).Trim();
    }

    private static Dictionary<string, object> NormalizeQuestionDict(Dictionary<string, object> q)
    {
        if (q.TryGetValue("question", out var questionObj) && questionObj is string questionStr)
        {
            q["question"] = NormalizePlainTextMath(questionStr);
        }
        if (q.TryGetValue("explanation", out var explObj) && explObj is string explStr)
        {
            q["explanation"] = NormalizePlainTextMath(explStr);
        }
        if (q.TryGetValue("correctAnswer", out var caObj) && caObj is string caStr)
        {
            q["correctAnswer"] = NormalizePlainTextMath(caStr);
        }
        if (q.TryGetValue("options", out var optionsObj))
        {
            if (optionsObj is JsonElement oe && oe.ValueKind == JsonValueKind.Array)
            {
                var list = new List<object>();
                foreach (var optEl in oe.EnumerateArray())
                {
                    var s = optEl.ValueKind == JsonValueKind.String ? optEl.GetString() ?? string.Empty : optEl.GetRawText();
                    list.Add(NormalizePlainTextMath(s));
                }
                q["options"] = list;
            }
            else if (optionsObj is List<object> optList)
            {
                var list = new List<object>();
                foreach (var o in optList)
                {
                    var s = o?.ToString() ?? string.Empty;
                    list.Add(NormalizePlainTextMath(s));
                }
                q["options"] = list;
            }
        }
        return q;
    }

    private static string NormalizePlainTextMath(string s)
    {
        var t = s ?? string.Empty;
        t = t.Replace("\r", " ").Replace("\n", " ");
        t = t.Replace("$", string.Empty);
        t = Regex.Replace(t, @"\\left|\\right", string.Empty);
        t = t.Replace("\\(", string.Empty).Replace("\\)", string.Empty)
             .Replace("\\[", string.Empty).Replace("\\]", string.Empty);
        t = Regex.Replace(t, @"\\frac\s*\{\s*([^}]+)\s*\}\s*\{\s*([^}]+)\s*\}", m => "(" + m.Groups[1].Value + ") / (" + m.Groups[2].Value + ")");
        t = Regex.Replace(t, @"\\sqrt\s*\{\s*([^}]+)\s*\}", m => "sqrt(" + m.Groups[1].Value + ")");
        t = Regex.Replace(t, @"([A-Za-z0-9])\s*\^\s*\{\s*([^}]+)\s*\}", "$1^$2");
        t = Regex.Replace(t, @"([A-Za-z0-9])\s*\^\s*([A-Za-z0-9]+)", "$1^$2");
        t = Regex.Replace(t, @"([A-Za-z])\s*_\s*\{\s*([^}]+)\s*\}", "$1_$2");
        t = Regex.Replace(t, @"([A-Za-z])\s*_\s*([A-Za-z0-9]+)", "$1_$2");
        t = Regex.Replace(t, @"\\int\s*_\{\s*([^}]+)\s*\}\s*\^\{\s*([^}]+)\s*\}", m => "integral from " + m.Groups[1].Value + " to " + m.Groups[2].Value + " of ");
        t = Regex.Replace(t, @"\\(sin|cos|tan|ln|log|lim|sum|alpha|beta|gamma|theta|pi)", m => m.Groups[1].Value);
        t = Regex.Replace(t, @"(?s)\\begin\{bmatrix\}(.+?)\\end\{bmatrix\}", m =>
        {
            var content = m.Groups[1].Value;
            var rows = Regex.Split(content, @"\\\\");
            var rowStrings = rows.Select(r => "[" + string.Join(", ", r.Split('&').Select(c => c.Trim())) + "]");
            return "[" + string.Join(", ", rowStrings) + "]";
        });
        t = t.Replace("\\", string.Empty);
        t = Regex.Replace(t, @"\s+", " ").Trim();
        return t;
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

    private ActivityStats AnalyzeActivities(List<Dictionary<string, object>> activities)
    {
        var stats = new ActivityStats();
        foreach (var activity in activities)
        {
            if (!activity.TryGetValue("type", out var typeObj)) continue;
            var type = typeObj?.ToString();
            switch (type?.ToLowerInvariant())
            {
                case "reading": stats.ReadingCount++; break;
                case "knowledgecheck": stats.KnowledgeCheckCount++; break;
                case "quiz": stats.QuizCount++; break;
            }
        }
        stats.TotalActivities = activities.Count;
        return stats;
    }

    private void LogWeekStatistics(int weekNumber, ActivityStats stats)
    {
        _logger.LogInformation("Week {Week} Stats: {Total}A ({R}R, {KC}KC, {Q}Quiz)",
            weekNumber, stats.TotalActivities, stats.ReadingCount, stats.KnowledgeCheckCount, stats.QuizCount);
    }

    private void ValidateWeekRequirements(int weekNumber, ActivityStats stats, string subjectName)
    {
        if (stats.TotalActivities < MinActivitiesPerStep)
            _logger.LogWarning("⚠️ Week {Week}: Low activity count ({Count})", weekNumber, stats.TotalActivities);
        if (stats.ReadingCount < 1) _logger.LogWarning("⚠️ Week {Week}: NO Readings!", weekNumber);
        if (stats.QuizCount < 1) _logger.LogWarning("⚠️ Week {Week}: NO Quiz!", weekNumber);
    }

    private bool HasEnrichedUrls(Dictionary<string, object> content)
    {
        try
        {
            if (content.TryGetValue("sessionSchedule", out var scheduleObj) ||
                content.TryGetValue("SessionSchedule", out scheduleObj))
            {
                if (scheduleObj is JsonElement je && je.ValueKind == JsonValueKind.Array)
                {
                    // Check first 3 sessions for suggestedUrl
                    foreach (var session in je.EnumerateArray().Take(3))
                    {
                        if (session.TryGetProperty("suggestedUrl", out var urlProp) &&
                            !string.IsNullOrWhiteSpace(urlProp.GetString()))
                        {
                            return true;
                        }
                    }
                }
            }
        }
        catch { }
        return false;
    }

    private class ActivityStats
    {
        public int TotalActivities { get; set; }
        public int ReadingCount { get; set; }
        public int KnowledgeCheckCount { get; set; }
        public int TotalKnowledgeCheckQuestions { get; set; }
        public int QuizCount { get; set; }
        public int TotalQuizQuestions { get; set; }
    }
}
