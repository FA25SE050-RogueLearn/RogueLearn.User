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

            // ⭐ FIX: If attempt doesn't exist yet (Not Started), return empty progress instead of throwing exception
            if (attempt == null)
            {
                _logger.LogInformation("ℹ️ No attempt found for Quest:{QuestId} - returning empty activity list", request.QuestId);

                // Parse activities but mark all as incomplete
                var allActivities = ExtractAndMapActivities(questStep.Content, Array.Empty<Guid>());

                return new CompletedActivitiesDto
                {
                    StepId = request.StepId,
                    Activities = allActivities,
                    CompletedCount = 0,
                    TotalCount = allActivities.Count
                };
            }

            // 3. Get step progress - if null, return empty progress (user just started this step)
            var stepProgress = await _stepProgressRepository.FirstOrDefaultAsync(
                sp => sp.AttemptId == attempt.Id && sp.StepId == request.StepId,
                cancellationToken);

            if (stepProgress is null)
            {
                _logger.LogInformation("ℹ️ No progress yet for Step:{StepId} - user just started this step", request.StepId);

                // Return empty progress with all activities marked as incomplete
                var allActivities = ExtractAndMapActivities(questStep.Content, Array.Empty<Guid>());

                var emptyResult = new CompletedActivitiesDto
                {
                    StepId = request.StepId,
                    Activities = allActivities,
                    CompletedCount = 0,
                    TotalCount = allActivities.Count
                };

                return emptyResult;
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

            _logger.LogInformation("✅ Completed activities: {Completed}/{Total}",
                result.CompletedCount, result.TotalCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error fetching completed activities for Step:{StepId}", request.StepId);
            throw;
        }
    }

    private List<ActivityProgressDto> ExtractAndMapActivities(object? content, Guid[] completedIds)
    {
        var activities = new List<ActivityProgressDto>();
        var completedSet = completedIds.ToHashSet();

        if (content == null) return activities;

        try
        {
            // Handle JObject
            if (content.GetType().Name == "JObject")
            {
                var jObjectJson = content.ToString();
                using (var doc = JsonDocument.Parse(jObjectJson))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("activities", out var activitiesElement) &&
                        activitiesElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var activityElement in activitiesElement.EnumerateArray())
                        {
                            var activity = ParseActivityElement(activityElement);
                            if (activity != null)
                            {
                                activity.IsCompleted = completedSet.Contains(activity.ActivityId);
                                activities.Add(activity);
                            }
                        }
                    }
                }
                return activities;
            }

            // Handle Dictionary
            if (content is Dictionary<string, object> contentDict &&
                contentDict.TryGetValue("activities", out var activitiesObj) &&
                activitiesObj is List<object> activitiesList)
            {
                foreach (var activityObj in activitiesList)
                {
                    if (activityObj is Dictionary<string, object> activityDict)
                    {
                        var activity = ParseActivityDict(activityDict);
                        if (activity != null)
                        {
                            activity.IsCompleted = completedSet.Contains(activity.ActivityId);
                            activities.Add(activity);
                        }
                    }
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

    private ActivityProgressDto? ParseActivityDict(Dictionary<string, object> activityDict)
    {
        if (!activityDict.TryGetValue("activityId", out var idObj) ||
            !Guid.TryParse(idObj.ToString(), out var activityId))
        {
            return null;
        }

        var activity = new ActivityProgressDto { ActivityId = activityId };

        if (activityDict.TryGetValue("type", out var typeObj))
        {
            activity.ActivityType = typeObj.ToString() ?? "Unknown";
        }

        if (activityDict.TryGetValue("payload", out var payloadObj) &&
            payloadObj is Dictionary<string, object> payload)
        {
            if (payload.TryGetValue("experiencePoints", out var xpObj) &&
                int.TryParse(xpObj.ToString(), out var xp))
            {
                activity.ExperiencePoints = xp;
            }

            if (payload.TryGetValue("skillId", out var skillIdObj) &&
                Guid.TryParse(skillIdObj.ToString(), out var skillId))
            {
                activity.SkillId = skillId;
            }

            activity.Title = activity.ActivityType switch
            {
                "Reading" => payload.TryGetValue("articleTitle", out var titleObj)
                    ? titleObj.ToString() : "Reading Activity",
                "Quiz" => "Quiz",
                "KnowledgeCheck" => payload.TryGetValue("topic", out var topicObj)
                    ? topicObj.ToString() : "Knowledge Check",
                "Coding" => payload.TryGetValue("topic", out var codingTopicObj)
                    ? codingTopicObj.ToString() : "Coding Challenge",
                _ => "Activity"
            };
        }

        return activity;
    }
}