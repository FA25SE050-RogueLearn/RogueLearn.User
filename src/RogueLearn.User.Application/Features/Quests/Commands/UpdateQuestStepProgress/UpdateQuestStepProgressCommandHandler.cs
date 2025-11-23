// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/UpdateQuestStepProgress/UpdateQuestStepProgressCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestStepProgress;

public class UpdateQuestStepProgressCommandHandler : IRequestHandler<UpdateQuestStepProgressCommand>
{
    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly IUserQuestStepProgressRepository _stepProgressRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly IQuestRepository _questRepository;
    private readonly ILogger<UpdateQuestStepProgressCommandHandler> _logger;

    public UpdateQuestStepProgressCommandHandler(
        IUserQuestAttemptRepository attemptRepository,
        IUserQuestStepProgressRepository stepProgressRepository,
        IQuestStepRepository questStepRepository,
        IQuestRepository questRepository,
        ILogger<UpdateQuestStepProgressCommandHandler> logger)
    {
        _attemptRepository = attemptRepository;
        _stepProgressRepository = stepProgressRepository;
        _questStepRepository = questStepRepository;
        _questRepository = questRepository;
        _logger = logger;
    }

    public async Task Handle(UpdateQuestStepProgressCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "🎯 UpdateQuestStepProgress: Checking step completion for User:{AuthUserId}, Quest:{QuestId}, Step:{StepId}",
            request.AuthUserId, request.QuestId, request.StepId);

        // 1. Fetch the QuestStep
        var questStep = await _questStepRepository.GetByIdAsync(request.StepId, cancellationToken);
        if (questStep is null || questStep.QuestId != request.QuestId)
        {
            _logger.LogError("❌ QuestStep {StepId} not found or belongs to different quest", request.StepId);
            throw new NotFoundException("QuestStep", request.StepId);
        }

        // 2. Fetch or create UserQuestAttempt
        var attempt = await _attemptRepository.FirstOrDefaultAsync(
            a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
            cancellationToken);

        if (attempt == null)
        {
            attempt = new UserQuestAttempt
            {
                AuthUserId = request.AuthUserId,
                QuestId = request.QuestId,
                Status = QuestAttemptStatus.InProgress,
                StartedAt = DateTimeOffset.UtcNow
            };
            attempt = await _attemptRepository.AddAsync(attempt, cancellationToken);
            _logger.LogInformation("✅ Created new UserQuestAttempt {AttemptId} for Quest {QuestId}",
                attempt.Id, request.QuestId);
        }

        // 3. Mark parent Quest as InProgress if needed
        var parentQuest = await _questRepository.GetByIdAsync(request.QuestId, cancellationToken)
            ?? throw new NotFoundException("Quest", request.QuestId);

        if (parentQuest.Status == QuestStatus.NotStarted)
        {
            parentQuest.Status = QuestStatus.InProgress;
            await _questRepository.UpdateAsync(parentQuest, cancellationToken);
            _logger.LogInformation("✅ Parent Quest {QuestId} status updated to InProgress", parentQuest.Id);
        }

        // 4. Fetch or create UserQuestStepProgress for this step
        var stepProgress = await _stepProgressRepository.FirstOrDefaultAsync(
            sp => sp.AttemptId == attempt.Id && sp.StepId == request.StepId,
            cancellationToken);

        if (stepProgress == null)
        {
            stepProgress = new UserQuestStepProgress
            {
                AttemptId = attempt.Id,
                StepId = request.StepId,
                Status = StepCompletionStatus.InProgress,
                StartedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                AttemptsCount = 1,
                CompletedActivityIds = Array.Empty<Guid>()
            };
            stepProgress = await _stepProgressRepository.AddAsync(stepProgress, cancellationToken);
            _logger.LogInformation("✅ Created new UserQuestStepProgress {ProgressId} for Step {StepId}",
                stepProgress.Id, request.StepId);
        }

        // 5. ⭐ CRITICAL: DO NOT AWARD XP HERE!
        // XP is awarded by UpdateQuestActivityProgressCommandHandler when activity is completed
        // This handler ONLY updates step/quest status based on activity completion

        _logger.LogInformation("🔍 Checking step completion status for Step {StepId}", request.StepId);

        // Check if all activities in this step are now completed
        await CheckAndUpdateStepCompletion(questStep, stepProgress, attempt, cancellationToken);

        _logger.LogInformation("✅ Successfully updated step progress for Step:{StepId}", request.StepId);
    }

    /// <summary>
    /// Checks if all activities in a step are completed and updates the step/quest status accordingly.
    /// Called after an activity is completed (by UpdateQuestActivityProgressCommandHandler).
    /// </summary>
    private async Task CheckAndUpdateStepCompletion(
        QuestStep questStep,
        UserQuestStepProgress stepProgress,
        UserQuestAttempt attempt,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("📊 Step progress: {Completed}/{Total} activities completed",
            stepProgress.CompletedActivityIds?.Length ?? 0,
            CountActivitiesInContent(questStep.Content));

        // Extract all activity IDs from the step content
        var allActivityIds = ExtractActivityIdsFromContent(questStep.Content);
        var completedActivityIds = stepProgress.CompletedActivityIds?.ToHashSet() ?? new HashSet<Guid>();

        _logger.LogInformation("📋 Total activities in step: {Total}, Completed: {Completed}",
            allActivityIds.Count, completedActivityIds.Count);

        // If all activities are completed, mark step as complete
        if (allActivityIds.Count > 0 && allActivityIds.IsSubsetOf(completedActivityIds))
        {
            if (stepProgress.Status != StepCompletionStatus.Completed)
            {
                stepProgress.Status = StepCompletionStatus.Completed;
                stepProgress.CompletedAt = DateTimeOffset.UtcNow;
                stepProgress.UpdatedAt = DateTimeOffset.UtcNow;
                await _stepProgressRepository.UpdateAsync(stepProgress, cancellationToken);
                _logger.LogInformation("🎉 Step {StepId} marked as COMPLETED ({Completed}/{Total} activities)",
                    questStep.Id, completedActivityIds.Count, allActivityIds.Count);
            }

            // Now check if the entire quest is complete
            await CheckAndUpdateQuestCompletion(attempt, cancellationToken);
        }
        else
        {
            // Some activities still pending
            if (stepProgress.Status != StepCompletionStatus.InProgress)
            {
                stepProgress.Status = StepCompletionStatus.InProgress;
                stepProgress.UpdatedAt = DateTimeOffset.UtcNow;
                await _stepProgressRepository.UpdateAsync(stepProgress, cancellationToken);
            }
            _logger.LogInformation("⏳ Step {StepId} remains InProgress ({Completed}/{Total} activities)",
                questStep.Id, completedActivityIds.Count, allActivityIds.Count);
        }
    }

    /// <summary>
    /// Checks if all steps in a quest are completed and updates the quest status accordingly.
    /// </summary>
    private async Task CheckAndUpdateQuestCompletion(
        UserQuestAttempt attempt,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔍 Checking quest completion for Attempt {AttemptId}", attempt.Id);

        // Fetch all steps in this quest
        var allStepsInQuest = (await _questStepRepository.FindByQuestIdAsync(attempt.QuestId, cancellationToken))
            .ToList();
        var totalStepsInQuest = allStepsInQuest.Count;

        if (totalStepsInQuest == 0)
        {
            _logger.LogWarning("⚠️ Quest {QuestId} has no steps", attempt.QuestId);
            return;
        }

        // Fetch all step progress records for this attempt
        var progressForQuestSteps = (await _stepProgressRepository.FindAsync(
            sp => sp.AttemptId == attempt.Id,
            cancellationToken
        )).ToList();

        // Count completed steps (in-memory filtering to avoid enum serialization issues)
        var completedStepsInAttempt = progressForQuestSteps
            .Count(sp => sp.Status == StepCompletionStatus.Completed);

        _logger.LogInformation("📊 Quest completion check: {Completed}/{Total} steps completed",
            completedStepsInAttempt, totalStepsInQuest);

        // If all steps are complete, mark quest and attempt as complete
        if (completedStepsInAttempt >= totalStepsInQuest)
        {
            if (attempt.Status != QuestAttemptStatus.Completed)
            {
                attempt.Status = QuestAttemptStatus.Completed;
                attempt.CompletedAt = DateTimeOffset.UtcNow;
                await _attemptRepository.UpdateAsync(attempt, cancellationToken);
                _logger.LogInformation("🏆 UserQuestAttempt {AttemptId} marked as COMPLETED", attempt.Id);
            }

            // Update parent quest status
            var parentQuest = await _questRepository.GetByIdAsync(attempt.QuestId, cancellationToken);
            if (parentQuest != null && parentQuest.Status != QuestStatus.Completed)
            {
                parentQuest.Status = QuestStatus.Completed;
                await _questRepository.UpdateAsync(parentQuest, cancellationToken);
                _logger.LogInformation("🏆 Parent Quest {QuestId} marked as COMPLETED", parentQuest.Id);
            }
        }
        else
        {
            // Some steps still pending - keep quest as InProgress
            if (attempt.Status != QuestAttemptStatus.InProgress)
            {
                attempt.Status = QuestAttemptStatus.InProgress;
                attempt.UpdatedAt = DateTimeOffset.UtcNow;
                await _attemptRepository.UpdateAsync(attempt, cancellationToken);
            }
            _logger.LogInformation("⏳ Quest {QuestId} remains InProgress ({Completed}/{Total} steps)",
                attempt.QuestId, completedStepsInAttempt, totalStepsInQuest);
        }
    }

    /// <summary>
    /// Extracts all activity IDs from the quest step's JSON content.
    /// Handles both Dictionary and JObject content types.
    /// </summary>
    private HashSet<Guid> ExtractActivityIdsFromContent(object? content)
    {
        var activityIds = new HashSet<Guid>();

        if (content == null)
        {
            _logger.LogWarning("⚠️ Content is null");
            return activityIds;
        }

        try
        {
            // Handle JObject from EF Core JSONB
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
                            if (activityElement.TryGetProperty("activityId", out var idElement) &&
                                Guid.TryParse(idElement.GetString(), out var id))
                            {
                                activityIds.Add(id);
                            }
                        }
                    }
                }
                return activityIds;
            }

            // Handle Dictionary format
            if (content is Dictionary<string, object> contentDict &&
                contentDict.TryGetValue("activities", out var activitiesObj) &&
                activitiesObj is List<object> activitiesList)
            {
                foreach (var activityObj in activitiesList)
                {
                    if (activityObj is Dictionary<string, object> activityDict &&
                        activityDict.TryGetValue("activityId", out var idObj) &&
                        Guid.TryParse(idObj.ToString(), out var id))
                    {
                        activityIds.Add(id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error extracting activity IDs from content");
        }

        return activityIds;
    }

    /// <summary>
    /// Counts the number of activities in the quest step's content.
    /// </summary>
    private int CountActivitiesInContent(object? content)
    {
        if (content == null) return 0;

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
                        return activitiesElement.GetArrayLength();
                    }
                }
                return 0;
            }

            // Handle Dictionary
            if (content is Dictionary<string, object> contentDict &&
                contentDict.TryGetValue("activities", out var activitiesObj) &&
                activitiesObj is List<object> activitiesList)
            {
                return activitiesList.Count;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error counting activities");
        }

        return 0;
    }
}