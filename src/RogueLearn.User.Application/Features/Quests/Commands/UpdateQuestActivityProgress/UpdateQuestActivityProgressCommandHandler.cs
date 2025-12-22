using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;

namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestActivityProgress;

public class UpdateQuestActivityProgressCommandHandler : IRequestHandler<UpdateQuestActivityProgressCommand>
{
    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly IUserQuestStepProgressRepository _stepProgressRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly ISubjectSkillMappingRepository _subjectSkillMappingRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<UpdateQuestActivityProgressCommandHandler> _logger;

    public UpdateQuestActivityProgressCommandHandler(
        IUserQuestAttemptRepository attemptRepository,
        IUserQuestStepProgressRepository stepProgressRepository,
        IQuestStepRepository questStepRepository,
        ISubjectSkillMappingRepository subjectSkillMappingRepository,
        IQuestRepository questRepository,
        IMediator mediator,
        ILogger<UpdateQuestActivityProgressCommandHandler> logger)
    {
        _attemptRepository = attemptRepository;
        _stepProgressRepository = stepProgressRepository;
        _questStepRepository = questStepRepository;
        _subjectSkillMappingRepository = subjectSkillMappingRepository;
        _questRepository = questRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(UpdateQuestActivityProgressCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch the Quest Step
        var step = await _questStepRepository.GetByIdAsync(request.StepId, cancellationToken);
        if (step == null) throw new NotFoundException($"Quest Step {request.StepId} not found");

        // 2. Fetch the User's Quest Attempt
        var attempt = await _attemptRepository.FirstOrDefaultAsync(
            a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
            cancellationToken);

        if (attempt == null) throw new NotFoundException("Quest not started.");

        // 3. Resolve Difficulty (Now using stored value)
        string currentDifficulty = attempt.AssignedDifficulty ?? "Standard";

        // 4. Get or Create Step Progress
        var stepProgress = await _stepProgressRepository.FirstOrDefaultAsync(
            p => p.AttemptId == attempt.Id && p.StepId == request.StepId,
            cancellationToken);

        bool isNewStepProgress = false;

        if (stepProgress == null)
        {
            isNewStepProgress = true;
            stepProgress = new UserQuestStepProgress
            {
                Id = Guid.NewGuid(),
                AttemptId = attempt.Id,
                StepId = request.StepId,
                Status = StepCompletionStatus.InProgress,
                CompletedActivityIds = new Guid[] { },
                StartedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }

        var completedList = stepProgress.CompletedActivityIds != null
            ? stepProgress.CompletedActivityIds.ToList()
            : new List<Guid>();

        bool isAlreadyCompleted = completedList.Contains(request.ActivityId);
        bool progressChanged = false;

        if (request.Status == StepCompletionStatus.Completed && !isAlreadyCompleted)
        {
            completedList.Add(request.ActivityId);
            stepProgress.CompletedActivityIds = completedList.ToArray();
            progressChanged = true;

            var (activityXp, activitySkillId, activityTitle) = ExtractActivityDetails(step.Content, request.ActivityId);

            // This is the core logic for XP capping.
            // It ensures that a user's total experience for a quest does not exceed the
            // maximum possible for their currently assigned difficulty level. This is crucial for
            // re-attempts at higher difficulties, as it prevents earning unlimited XP.
            if (activityXp > 0)
            {
                // Fetch all steps for the quest to calculate the XP cap for the current difficulty.
                var allQuestSteps = await _questStepRepository.GetByQuestIdAsync(request.QuestId, cancellationToken);
                var maxExperienceForDifficulty = allQuestSteps
                    .Where(s => string.Equals(s.DifficultyVariant, attempt.AssignedDifficulty, StringComparison.OrdinalIgnoreCase))
                    .Sum(s => s.ExperiencePoints);

                // Calculate the potential new total XP and cap it at the maximum for this difficulty.
                var potentialNewTotalXp = attempt.TotalExperienceEarned + activityXp;
                var newTotalXp = Math.Min(potentialNewTotalXp, maxExperienceForDifficulty);

                // This logic correctly handles both UPGRADE and DOWNGRADE scenarios:
                // 1. UPGRADE: If a user earned 1000 XP on Standard (cap 1000) and moves to Challenging (cap 1500),
                //    they can earn up to 500 additional XP. `newTotalXp` will be greater than `attempt.TotalExperienceEarned`,
                //    so the `if` block executes.
                // 2. DOWNGRADE: If a user earned 1500 XP on Challenging and moves to Standard (cap 1000),
                //    `newTotalXp` will be capped at 1000. The condition `if (1000 > 1500)` will be FALSE.
                //    This correctly prevents any new XP from being awarded and preserves their higher score of 1500.
                if (newTotalXp > attempt.TotalExperienceEarned)
                {
                    var xpGained = newTotalXp - attempt.TotalExperienceEarned;
                    attempt.TotalExperienceEarned = newTotalXp;
                    await _attemptRepository.UpdateAsync(attempt, cancellationToken);

                    // The XP event is dispatched with the actual XP gained after capping.
                    if (activitySkillId.HasValue)
                    {
                        await _mediator.Send(new IngestXpEventCommand
                        {
                            AuthUserId = request.AuthUserId,
                            SkillId = activitySkillId.Value,
                            Points = xpGained,
                            SourceService = "QuestSystem",
                            SourceType = "ActivityComplete",
                            SourceId = request.ActivityId,
                            Reason = $"Completed: {activityTitle ?? "Quest Activity"}"
                        }, cancellationToken);
                    }
                    else
                    {
                        // Fallback logic for primary skill
                        var quest = await _questRepository.GetByIdAsync(request.QuestId, cancellationToken);
                        if (quest != null && quest.SubjectId.HasValue)
                        {
                            var skillMappings = await _subjectSkillMappingRepository.GetMappingsBySubjectIdsAsync(new[] { quest.SubjectId.Value }, cancellationToken);
                            var primaryMapping = skillMappings.OrderByDescending(m => m.RelevanceWeight).FirstOrDefault();
                            if (primaryMapping != null)
                            {
                                await _mediator.Send(new IngestXpEventCommand
                                {
                                    AuthUserId = request.AuthUserId,
                                    SkillId = primaryMapping.SkillId,
                                    Points = xpGained,
                                    SourceService = "QuestSystem",
                                    SourceType = "ActivityComplete",
                                    SourceId = request.ActivityId,
                                    Reason = $"Completed: {activityTitle ?? "Quest Activity"} (Primary Skill Fallback)"
                                }, cancellationToken);
                            }
                        }
                    }
                }
            }

            bool isQuiz = IsQuizActivity(step, request.ActivityId);
            bool isCompleteByCount = CheckIfStepIsComplete(step, completedList);

            if (isQuiz || isCompleteByCount)
            {
                stepProgress.Status = StepCompletionStatus.Completed;
                stepProgress.CompletedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                stepProgress.Status = StepCompletionStatus.InProgress;
            }
        }
        else if (request.Status != StepCompletionStatus.Completed && isAlreadyCompleted)
        {
            completedList.Remove(request.ActivityId);
            stepProgress.CompletedActivityIds = completedList.ToArray();
            progressChanged = true;
            stepProgress.Status = StepCompletionStatus.InProgress;
            stepProgress.CompletedAt = null;
        }

        if (isNewStepProgress)
        {
            stepProgress.UpdatedAt = DateTimeOffset.UtcNow;
            await _stepProgressRepository.AddAsync(stepProgress, cancellationToken);
        }
        else if (progressChanged)
        {
            stepProgress.UpdatedAt = DateTimeOffset.UtcNow;
            await _stepProgressRepository.UpdateAsync(stepProgress, cancellationToken);
        }

        // Pass the locked difficulty to the percentage updater
        await UpdateQuestCompletionPercentage(attempt, request.QuestId, currentDifficulty, cancellationToken);
    }

    private async Task UpdateQuestCompletionPercentage(UserQuestAttempt attempt, Guid questId, string difficultyVariant, CancellationToken cancellationToken)
    {
        try
        {
            var allSteps = await _questStepRepository.GetByQuestIdAsync(questId, cancellationToken);
            // Filter steps using the SNAPSHOTTED difficulty
            var userTrackSteps = allSteps
                .Where(s => string.Equals(s.DifficultyVariant, difficultyVariant, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var allProgress = await _stepProgressRepository.GetByAttemptIdAsync(attempt.Id, cancellationToken);
            var progressDict = allProgress.ToDictionary(p => p.StepId);

            int totalActivities = 0;
            int completedActivities = 0;

            foreach (var trackStep in userTrackSteps)
            {
                int stepActivityCount = ExtractActivityCount(trackStep.Content);
                totalActivities += stepActivityCount;

                if (progressDict.TryGetValue(trackStep.Id, out var prog))
                {
                    if (prog.Status == StepCompletionStatus.Completed)
                    {
                        completedActivities += stepActivityCount;
                    }
                    else
                    {
                        completedActivities += prog.CompletedActivityIds?.Length ?? 0;
                    }
                }
            }

            if (totalActivities > 0)
            {
                attempt.CompletionPercentage = Math.Round((decimal)completedActivities / totalActivities * 100, 2);
                if (attempt.CompletionPercentage >= 100 && attempt.Status != QuestAttemptStatus.Completed)
                {
                    attempt.Status = QuestAttemptStatus.Completed;
                    attempt.CompletedAt = DateTimeOffset.UtcNow;
                }
            }

            attempt.UpdatedAt = DateTimeOffset.UtcNow;
            await _attemptRepository.UpdateAsync(attempt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update completion percentage for attempt {AttemptId}", attempt.Id);
        }
    }

    private (int xp, Guid? skillId, string? title) ExtractActivityDetails(object? content, Guid targetActivityId)
    {
        try
        {
            var jsonString = ExtractJsonString(content);
            using var doc = JsonDocument.Parse(jsonString);
            var activities = TryGetActivitiesElement(doc);

            if (activities.HasValue)
            {
                foreach (var activity in activities.Value.EnumerateArray())
                {
                    var idEl = GetPropertyCaseInsensitive(activity, "activityId");

                    if (idEl != null && Guid.TryParse(idEl.Value.GetString(), out var id) && id == targetActivityId)
                    {
                        int xp = 0;
                        Guid? skillId = null;
                        string? title = "Activity";

                        var skillEl = GetPropertyCaseInsensitive(activity, "skillId");
                        if (skillEl != null && Guid.TryParse(skillEl.Value.GetString(), out var sid))
                        {
                            skillId = sid;
                        }

                        var payload = GetPropertyCaseInsensitive(activity, "payload");
                        if (payload.HasValue)
                        {
                            var xpEl = GetPropertyCaseInsensitive(payload.Value, "experiencePoints");
                            if (xpEl != null)
                            {
                                if (xpEl.Value.ValueKind == JsonValueKind.Number && xpEl.Value.TryGetInt32(out var points))
                                {
                                    xp = points;
                                }
                                else if (xpEl.Value.ValueKind == JsonValueKind.String && int.TryParse(xpEl.Value.GetString(), out var pointsParsed))
                                {
                                    xp = pointsParsed;
                                }
                            }

                            var titleEl = GetPropertyCaseInsensitive(payload.Value, "articleTitle")
                                       ?? GetPropertyCaseInsensitive(payload.Value, "topic");
                            if (titleEl != null)
                            {
                                title = titleEl.Value.GetString();
                            }
                        }
                        return (xp, skillId, title);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CRASH in ExtractActivityDetails for ActivityId {Id}", targetActivityId);
        }
        return (0, null, null);
    }

    private static string ExtractJsonString(object? content)
    {
        if (content is null) return "{}";
        if (content is string s) return s;
        if (content is JsonElement je) return je.GetRawText();
        var typeName = content.GetType().Name;
        if (typeName == "JObject" || typeName == "JArray" || typeName == "JToken") return content.ToString()!;
        return JsonSerializer.Serialize(content);
    }

    private static JsonElement? TryGetActivitiesElement(JsonDocument doc)
    {
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Array) return root;
        if (root.ValueKind == JsonValueKind.Object) return GetPropertyCaseInsensitive(root, "activities");
        return null;
    }

    private static JsonElement? GetPropertyCaseInsensitive(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase)) return property.Value;
        }
        return null;
    }

    private bool IsQuizActivity(QuestStep step, Guid activityId)
    {
        try
        {
            var jsonString = ExtractJsonString(step.Content);
            using var doc = JsonDocument.Parse(jsonString);
            var activitiesElement = TryGetActivitiesElement(doc);
            if (activitiesElement == null) return false;
            foreach (var activity in activitiesElement.Value.EnumerateArray())
            {
                var idEl = GetPropertyCaseInsensitive(activity, "activityId");
                if (idEl != null && Guid.TryParse(idEl.Value.GetString(), out var id) && id == activityId)
                {
                    var typeEl = GetPropertyCaseInsensitive(activity, "type");
                    return typeEl != null && string.Equals(typeEl.Value.GetString(), "Quiz", StringComparison.OrdinalIgnoreCase);
                }
            }
        }
        catch { }
        return false;
    }

    private bool CheckIfStepIsComplete(QuestStep step, List<Guid> completedActivityIds)
    {
        try
        {
            var jsonString = ExtractJsonString(step.Content);
            using var doc = JsonDocument.Parse(jsonString);
            var activitiesElement = TryGetActivitiesElement(doc);
            if (activitiesElement.HasValue)
            {
                int total = activitiesElement.Value.GetArrayLength();
                int matched = 0;
                foreach (var activity in activitiesElement.Value.EnumerateArray())
                {
                    var idEl = GetPropertyCaseInsensitive(activity, "activityId");
                    if (idEl != null && Guid.TryParse(idEl.Value.GetString(), out var guid) && completedActivityIds.Contains(guid))
                    {
                        matched++;
                    }
                }
                return matched >= total;
            }
        }
        catch { }
        return false;
    }

    private int ExtractActivityCount(object? content)
    {
        try
        {
            var jsonString = ExtractJsonString(content);
            using var doc = JsonDocument.Parse(jsonString);
            var activitiesElement = TryGetActivitiesElement(doc);
            return activitiesElement?.GetArrayLength() ?? 0;
        }
        catch { return 0; }
    }
}