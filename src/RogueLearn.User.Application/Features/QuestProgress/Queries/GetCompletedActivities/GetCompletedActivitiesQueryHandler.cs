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
            // 1. Get quest step to access its content (the list of activities).
            var questStep = await _questStepRepository.GetByIdAsync(request.StepId, cancellationToken)
                ?? throw new NotFoundException("QuestStep", request.StepId);

            if (questStep.QuestId != request.QuestId)
            {
                throw new NotFoundException("QuestStep does not belong to this quest");
            }

            // 2. Get the user's specific attempt for this quest. This is the root of their progress.
            var attempt = await _attemptRepository.FirstOrDefaultAsync(
                a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
                cancellationToken);

            // If no attempt record exists, it means the user has never started this quest.
            // In this case, we return a list of all activities for the step, all marked as incomplete.
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

            // 3. Look for a progress record for this specific step within the user's attempt.
            var stepProgress = await _stepProgressRepository.FirstOrDefaultAsync(
                sp => sp.AttemptId == attempt.Id && sp.StepId == request.StepId,
                cancellationToken);

            // If no step progress exists, it means the user has started the quest but not this particular step.
            // This can happen after a difficulty upgrade where old progress was cleared. We return all activities as incomplete.
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

            // 4. If progress is found, parse the activities from the quest step's content and use the
            // 'completed_activity_ids' from the progress record to determine which are complete.
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

        _logger.LogInformation("📋 ExtractAndMapActivities: Content is {ContentType}, CompletedIds count: {CompletedCount}",
            content?.GetType().Name ?? "null", completedIds.Length);

        if (content == null)
        {
            _logger.LogWarning("📋 ExtractAndMapActivities: Content is NULL, returning empty list");
            return activities;
        }

        try
        {
            // Robust extraction of JSON string from unknown object type
            var jsonString = ExtractJsonString(content);

            _logger.LogInformation("📋 ExtractAndMapActivities: JSON string length: {Length}, Preview: {Preview}",
                jsonString.Length, jsonString.Length > 200 ? jsonString[..200] + "..." : jsonString);

            if (string.IsNullOrWhiteSpace(jsonString))
            {
                _logger.LogWarning("📋 ExtractAndMapActivities: JSON string is empty");
                return activities;
            }

            using (var doc = JsonDocument.Parse(jsonString))
            {
                var activitiesElement = TryGetActivitiesElement(doc);

                if (activitiesElement.HasValue)
                {
                    var activityCount = 0;
                    foreach (var activityElement in activitiesElement.Value.EnumerateArray())
                    {
                        activityCount++;
                        var activity = ParseActivityElement(activityElement, activityCount);
                        if (activity != null)
                        {
                            activity.IsCompleted = completedSet.Contains(activity.ActivityId);
                            activities.Add(activity);
                            _logger.LogInformation("📋 Activity {Index}: Id={ActivityId}, Type={Type}, IsCompleted={IsCompleted}",
                                activityCount, activity.ActivityId, activity.ActivityType, activity.IsCompleted);
                        }
                        else
                        {
                            _logger.LogWarning("📋 Activity {Index}: Failed to parse activity element", activityCount);
                        }
                    }
                    _logger.LogInformation("📋 ExtractAndMapActivities: Found {RawCount} elements, parsed {ParsedCount} activities",
                        activityCount, activities.Count);
                }
                else
                {
                    _logger.LogWarning("📋 No 'activities' element found in content: {JsonPreview}",
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

    private ActivityProgressDto? ParseActivityElement(JsonElement activityElement, int index)
    {
        if (activityElement.ValueKind != JsonValueKind.Object)
        {
            _logger.LogWarning("📋 ParseActivityElement[{Index}]: Element is not an object, kind={Kind}",
                index, activityElement.ValueKind);
            return null;
        }

        // Log all property names for debugging
        var propertyNames = string.Join(", ", activityElement.EnumerateObject().Select(p => p.Name));
        _logger.LogInformation("📋 ParseActivityElement[{Index}]: Properties found: [{Properties}]", index, propertyNames);

        // Case-insensitive lookup for activityId
        var idElement = GetPropertyCaseInsensitive(activityElement, "activityId");
        if (idElement == null)
        {
            _logger.LogWarning("📋 ParseActivityElement[{Index}]: 'activityId' property not found (case-insensitive)", index);
            return null;
        }

        if (!Guid.TryParse(idElement.Value.GetString(), out var activityId))
        {
            _logger.LogWarning("📋 ParseActivityElement[{Index}]: Failed to parse activityId: {Value}",
                index, idElement.Value.GetString());
            return null;
        }

        var activity = new ActivityProgressDto { ActivityId = activityId };

        // Case-insensitive lookup for type
        var typeElement = GetPropertyCaseInsensitive(activityElement, "type");
        if (typeElement != null)
        {
            activity.ActivityType = typeElement.Value.GetString() ?? "Unknown";
        }

        // Case-insensitive lookup for payload
        var payloadElement = GetPropertyCaseInsensitive(activityElement, "payload");
        if (payloadElement != null)
        {
            var xpElement = GetPropertyCaseInsensitive(payloadElement.Value, "experiencePoints");
            if (xpElement != null && xpElement.Value.TryGetInt32(out var xp))
            {
                activity.ExperiencePoints = xp;
            }

            var skillIdElement = GetPropertyCaseInsensitive(payloadElement.Value, "skillId");
            if (skillIdElement != null && Guid.TryParse(skillIdElement.Value.GetString(), out var skillId))
            {
                activity.SkillId = skillId;
            }

            // Get title based on type (case-insensitive property lookups)
            activity.Title = activity.ActivityType switch
            {
                "Reading" => GetPropertyCaseInsensitive(payloadElement.Value, "articleTitle")?.GetString() ?? "Reading Activity",
                "Quiz" => "Quiz",
                "KnowledgeCheck" => GetPropertyCaseInsensitive(payloadElement.Value, "topic")?.GetString() ?? "Knowledge Check",
                "Coding" => GetPropertyCaseInsensitive(payloadElement.Value, "topic")?.GetString() ?? "Coding Challenge",
                _ => "Activity"
            };
        }

        return activity;
    }

    /// <summary>
    /// Gets a property from a JSON element using case-insensitive matching.
    /// Supports both PascalCase and camelCase property names.
    /// </summary>
    private static JsonElement? GetPropertyCaseInsensitive(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return null;
    }
}