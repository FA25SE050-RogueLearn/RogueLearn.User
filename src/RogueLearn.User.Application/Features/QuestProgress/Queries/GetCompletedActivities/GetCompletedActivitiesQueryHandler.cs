// RogueLearn.User/src/RogueLearn.User.Application/Features/QuestProgress/Queries/GetCompletedActivities/GetCompletedActivitiesQueryHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.QuestProgress.Queries.GetCompletedActivities;

public class GetCompletedActivitiesQueryHandler : IRequestHandler<GetCompletedActivitiesQuery, CompletedActivitiesDto>
{
    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly IUserQuestStepProgressRepository _stepProgressRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly ILogger<GetCompletedActivitiesQueryHandler> _logger;

    public GetCompletedActivitiesQueryHandler(
        IUserQuestAttemptRepository attemptRepository,
        IUserQuestStepProgressRepository stepProgressRepository,
        IQuestStepRepository questStepRepository,
        ILogger<GetCompletedActivitiesQueryHandler> logger)
    {
        _attemptRepository = attemptRepository;
        _stepProgressRepository = stepProgressRepository;
        _questStepRepository = questStepRepository;
        _logger = logger;
    }

    public async Task<CompletedActivitiesDto> Handle(GetCompletedActivitiesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔍 Fetching completed activities for Step:{StepId}", request.StepId);

        try
        {
            // 1. Get quest step
            var questStep = await _questStepRepository.GetByIdAsync(request.StepId, cancellationToken)
                ?? throw new NotFoundException("QuestStep", request.StepId);

            if (questStep.QuestId != request.QuestId)
            {
                throw new NotFoundException("QuestStep does not belong to this quest");
            }

            // 2. Get user's attempt
            var attempt = await _attemptRepository.FirstOrDefaultAsync(
                a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
                cancellationToken);

            // If attempt doesn't exist yet (Not Started), return empty progress
            if (attempt == null)
            {
                _logger.LogInformation("ℹ️ No attempt found for Quest:{QuestId} - returning empty activity list", request.QuestId);
                var allActivities = ExtractAndMapActivities(questStep.Content, Array.Empty<Guid>());
                return new CompletedActivitiesDto
                {
                    StepId = request.StepId,
                    Activities = allActivities,
                    CompletedCount = 0,
                    TotalCount = allActivities.Count
                };
            }

            // 3. Get step progress
            var stepProgress = await _stepProgressRepository.FirstOrDefaultAsync(
                sp => sp.AttemptId == attempt.Id && sp.StepId == request.StepId,
                cancellationToken);

            if (stepProgress is null)
            {
                _logger.LogInformation("ℹ️ No progress yet for Step:{StepId} - user just started this step", request.StepId);
                var allActivities = ExtractAndMapActivities(questStep.Content, Array.Empty<Guid>());
                return new CompletedActivitiesDto
                {
                    StepId = request.StepId,
                    Activities = allActivities,
                    CompletedCount = 0,
                    TotalCount = allActivities.Count
                };
            }

            // 4. Parse activities from content and map with completion status
            var activities = ExtractAndMapActivities(questStep.Content, stepProgress.CompletedActivityIds ?? Array.Empty<Guid>());

            var result = new CompletedActivitiesDto
            {
                StepId = request.StepId,
                Activities = activities,
                CompletedCount = activities.Count(a => a.IsCompleted),
                TotalCount = activities.Count
            };

            _logger.LogInformation("✅ Completed activities: {Completed}/{Total}", result.CompletedCount, result.TotalCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error fetching completed activities for Step:{StepId}", request.StepId);
            throw;
        }
    }

    private static string ExtractJsonString(object? content)
    {
        if (content is null) return "{}";
        if (content is string s) return s;
        if (content is JsonElement je) return je.GetRawText();

        // Handle Newtonsoft JTypes (used by Supabase client)
        var typeName = content.GetType().Name;
        if (typeName == "JObject" || typeName == "JArray" || typeName == "JToken")
            return content.ToString()!;

        // Fallback for POCOs
        return JsonSerializer.Serialize(content);
    }

    private static JsonElement? TryGetActivitiesElement(JsonDocument doc)
    {
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
            return root;

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in root.EnumerateObject())
            {
                if (string.Equals(prop.Name, "activities", StringComparison.OrdinalIgnoreCase))
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                        return prop.Value;
                }
            }
        }
        return null;
    }

    private List<ActivityProgressDto> ExtractAndMapActivities(object? content, Guid[] completedIds)
    {
        var activities = new List<ActivityProgressDto>();
        var completedSet = completedIds.ToHashSet();

        if (content == null) return activities;

        try
        {
            // Robust extraction of JSON string from unknown object type
            var jsonString = ExtractJsonString(content);

            if (string.IsNullOrWhiteSpace(jsonString)) return activities;

            using (var doc = JsonDocument.Parse(jsonString))
            {
                var activitiesElement = TryGetActivitiesElement(doc);

                if (activitiesElement.HasValue)
                {
                    foreach (var activityElement in activitiesElement.Value.EnumerateArray())
                    {
                        var activity = ParseActivityElement(activityElement);
                        if (activity != null)
                        {
                            activity.IsCompleted = completedSet.Contains(activity.ActivityId);
                            activities.Add(activity);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("No 'activities' element found in content: {JsonPreview}",
                        jsonString.Length > 100 ? jsonString[..100] : jsonString);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error extracting activities");
        }

        return activities;
    }

    private ActivityProgressDto? ParseActivityElement(JsonElement activityElement)
    {
        if (activityElement.ValueKind != JsonValueKind.Object) return null;

        if (!activityElement.TryGetProperty("activityId", out var idElement) ||
            !Guid.TryParse(idElement.GetString(), out var activityId))
        {
            return null;
        }

        var activity = new ActivityProgressDto { ActivityId = activityId };

        if (activityElement.TryGetProperty("type", out var typeElement))
        {
            activity.ActivityType = typeElement.GetString() ?? "Unknown";
        }

        if (activityElement.TryGetProperty("payload", out var payloadElement))
        {
            if (payloadElement.TryGetProperty("experiencePoints", out var xpElement))
            {
                xpElement.TryGetInt32(out var xp);
                activity.ExperiencePoints = xp;
            }

            if (payloadElement.TryGetProperty("skillId", out var skillIdElement) &&
                Guid.TryParse(skillIdElement.GetString(), out var skillId))
            {
                activity.SkillId = skillId;
            }

            // Get title based on type
            activity.Title = activity.ActivityType switch
            {
                "Reading" => payloadElement.TryGetProperty("articleTitle", out var titleEl)
                    ? titleEl.GetString() : "Reading Activity",
                "Quiz" => "Quiz",
                "KnowledgeCheck" => payloadElement.TryGetProperty("topic", out var topicEl)
                    ? topicEl.GetString() : "Knowledge Check",
                "Coding" => payloadElement.TryGetProperty("topic", out var codingTopicEl)
                    ? codingTopicEl.GetString() : "Coding Challenge",
                _ => "Activity"
            };
        }

        return activity;
    }
}