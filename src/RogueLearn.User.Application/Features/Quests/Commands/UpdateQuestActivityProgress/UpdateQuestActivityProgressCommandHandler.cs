// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/UpdateQuestActivityProgress/UpdateQuestActivityProgressCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;
using System.Text.Json;
using RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;
using RogueLearn.User.Application.Common;

// MODIFIED: The namespace is updated to match the new command structure.
namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestActivityProgress;

public class UpdateQuestActivityProgressCommandHandler : IRequestHandler<UpdateQuestActivityProgressCommand>
{
    // Private record to help deserialize the activity payload for validation.
    private record ActivityPayload(JsonElement SkillId, JsonElement ExperiencePoints);

    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly IUserQuestStepProgressRepository _stepProgressRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<UpdateQuestActivityProgressCommandHandler> _logger;

    public UpdateQuestActivityProgressCommandHandler(
        IUserQuestAttemptRepository attemptRepository,
        IUserQuestStepProgressRepository stepProgressRepository,
        IQuestStepRepository questStepRepository,
        IQuestRepository questRepository,
        IMediator mediator,
        ILogger<UpdateQuestActivityProgressCommandHandler> logger)
    {
        _attemptRepository = attemptRepository;
        _stepProgressRepository = stepProgressRepository;
        _questStepRepository = questStepRepository;
        _questRepository = questRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(UpdateQuestActivityProgressCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating activity progress for User:{AuthUserId}, Quest:{QuestId}, Step:{StepId}, Activity:{ActivityId} to Status:{Status}",
            request.AuthUserId, request.QuestId, request.StepId, request.ActivityId, request.Status);

        var questStep = await _questStepRepository.GetByIdAsync(request.StepId, cancellationToken);
        if (questStep is null || questStep.QuestId != request.QuestId)
        {
            throw new NotFoundException("QuestStep (weekly module)", request.StepId);
        }

        // Find or create the overarching quest attempt.
        var attempt = await _attemptRepository.FirstOrDefaultAsync(
            a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
            cancellationToken) ?? await CreateNewAttemptAsync(request.AuthUserId, request.QuestId, cancellationToken);

        // Ensure the parent quest is marked as "InProgress" if this is the first action.
        await MarkParentQuestAsInProgressAsync(request.QuestId, cancellationToken);

        // Find or create the progress record for the entire weekly module (the quest_step).
        var stepProgress = await _stepProgressRepository.FirstOrDefaultAsync(
            sp => sp.AttemptId == attempt.Id && sp.StepId == request.StepId,
            cancellationToken) ?? await CreateNewStepProgressAsync(attempt.Id, request.StepId, cancellationToken);

        // Check for idempotency: if this specific activity is already complete, do nothing.
        if (stepProgress.CompletedActivityIds?.Contains(request.ActivityId) == true && request.Status == StepCompletionStatus.Completed)
        {
            _logger.LogInformation("Activity {ActivityId} is already completed for Step {StepId}. No action taken.", request.ActivityId, request.StepId);
            return;
        }

        // If the request is to mark the activity as completed, trigger the reward and check for module completion.
        if (request.Status == StepCompletionStatus.Completed)
        {
            // MODIFICATION: Pass the parent 'attempt' object which contains the correct AuthUserId.
            await CompleteActivityAndCheckForModuleCompletion(questStep, stepProgress, attempt, request.ActivityId, cancellationToken);
        }
        else
        {
            // For other statuses like "InProgress", just update the module's overall status.
            stepProgress.Status = request.Status;
            stepProgress.UpdatedAt = DateTimeOffset.UtcNow;
            await _stepProgressRepository.UpdateAsync(stepProgress, cancellationToken);
        }

        _logger.LogInformation("Successfully updated progress for Activity:{ActivityId} to Status:{Status}", request.ActivityId, request.Status);
    }

    private async Task<UserQuestAttempt> CreateNewAttemptAsync(Guid authUserId, Guid questId, CancellationToken cancellationToken)
    {
        var newAttempt = new UserQuestAttempt
        {
            AuthUserId = authUserId,
            QuestId = questId,
            Status = QuestAttemptStatus.InProgress,
            StartedAt = DateTimeOffset.UtcNow
        };
        var createdAttempt = await _attemptRepository.AddAsync(newAttempt, cancellationToken);
        _logger.LogInformation("Created new UserQuestAttempt {AttemptId} for Quest {QuestId}", createdAttempt.Id, questId);
        return createdAttempt;
    }

    private async Task MarkParentQuestAsInProgressAsync(Guid questId, CancellationToken cancellationToken)
    {
        var parentQuest = await _questRepository.GetByIdAsync(questId, cancellationToken)
            ?? throw new NotFoundException("Quest", questId);

        if (parentQuest.Status == QuestStatus.NotStarted)
        {
            parentQuest.Status = QuestStatus.InProgress;
            await _questRepository.UpdateAsync(parentQuest, cancellationToken);
            _logger.LogInformation("Parent Quest {QuestId} status updated to 'InProgress' due to first user action.", parentQuest.Id);
        }
    }

    private async Task<UserQuestStepProgress> CreateNewStepProgressAsync(Guid attemptId, Guid stepId, CancellationToken cancellationToken)
    {
        var newStepProgress = new UserQuestStepProgress
        {
            AttemptId = attemptId,
            StepId = stepId,
            Status = StepCompletionStatus.InProgress,
            StartedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            AttemptsCount = 1
        };
        await _stepProgressRepository.AddAsync(newStepProgress, cancellationToken);
        return newStepProgress;
    }

    // MODIFICATION: The method now accepts the 'UserQuestAttempt' object to get the correct AuthUserId.
    private async Task CompleteActivityAndCheckForModuleCompletion(QuestStep questStep, UserQuestStepProgress stepProgress, UserQuestAttempt attempt, Guid activityIdToComplete, CancellationToken cancellationToken)
    {
        // 1. Deserialize the module's content to access the activities.
        var contentDict = questStep.Content as Dictionary<string, object>;
        if (contentDict == null || !contentDict.TryGetValue("activities", out var activitiesObj) || activitiesObj is not List<object> activities)
        {
            _logger.LogError("QuestStep {StepId} has malformed content and does not contain an 'activities' array.", questStep.Id);
            throw new InvalidOperationException("Quest step content is invalid.");
        }

        // 2. Find the specific activity being completed.
        var activityToComplete = activities
            .OfType<Dictionary<string, object>>()
            .FirstOrDefault(act => act.TryGetValue("activityId", out var idObj) && idObj is string idStr && Guid.TryParse(idStr, out var id) && id == activityIdToComplete);

        if (activityToComplete == null)
        {
            throw new NotFoundException("Activity", activityIdToComplete);
        }

        // 3. Dispatch the XP event for this specific activity.
        if (activityToComplete.TryGetValue("payload", out var payloadObj) && payloadObj is Dictionary<string, object> payload)
        {
            if (payload.TryGetValue("skillId", out var skillIdObj) && Guid.TryParse(skillIdObj.ToString(), out var skillId) &&
                payload.TryGetValue("experiencePoints", out var xpObj) && int.TryParse(xpObj.ToString(), out var xp))
            {
                var xpEvent = new IngestXpEventCommand
                {
                    // MODIFICATION: This now correctly uses the AuthUserId from the parent 'attempt' object.
                    AuthUserId = attempt.AuthUserId,
                    SourceService = "QuestsService",
                    SourceType = SkillRewardSourceType.QuestComplete.ToString(),
                    SourceId = activityIdToComplete, // The source is now the activity itself.
                    SkillId = skillId,
                    Points = xp,
                    Reason = $"Completed activity in quest: {questStep.Title}"
                };
                await _mediator.Send(xpEvent, cancellationToken);
                _logger.LogInformation("Dispatched IngestXpEvent for SkillId '{SkillId}' from Activity '{ActivityId}'", skillId, activityIdToComplete);
            }
        }

        // 4. Update the progress record by adding the completed activity's ID.
        stepProgress.CompletedActivityIds = (stepProgress.CompletedActivityIds ?? Array.Empty<Guid>()).Append(activityIdToComplete).ToArray();
        stepProgress.UpdatedAt = DateTimeOffset.UtcNow;

        // 5. Check if the entire module (quest_step) is now complete.
        var allActivityIds = activities
            .OfType<Dictionary<string, object>>()
            .Select(act => act.TryGetValue("activityId", out var idObj) && idObj is string idStr && Guid.TryParse(idStr, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToHashSet();

        if (allActivityIds.IsSubsetOf(stepProgress.CompletedActivityIds.ToHashSet()))
        {
            stepProgress.Status = StepCompletionStatus.Completed;
            stepProgress.CompletedAt = DateTimeOffset.UtcNow;
            _logger.LogInformation("All activities for Step {StepId} are complete. Marking step as 'Completed'.", questStep.Id);

            // This is a good place to also check for overall quest completion.
            await CheckForOverallQuestCompletion(stepProgress.AttemptId, cancellationToken);
        }

        await _stepProgressRepository.UpdateAsync(stepProgress, cancellationToken);
    }

    private async Task CheckForOverallQuestCompletion(Guid attemptId, CancellationToken cancellationToken)
    {
        var attempt = await _attemptRepository.GetByIdAsync(attemptId, cancellationToken)
            ?? throw new NotFoundException("UserQuestAttempt", attemptId);

        var allStepsForQuest = await _questStepRepository.FindByQuestIdAsync(attempt.QuestId, cancellationToken);
        var allStepIdsForQuest = allStepsForQuest.Select(s => s.Id).ToHashSet();

        var completedStepsForAttempt = (await _stepProgressRepository.FindAsync(
            sp => sp.AttemptId == attemptId && sp.Status == StepCompletionStatus.Completed,
            cancellationToken
        )).Select(sp => sp.StepId).ToHashSet();

        if (allStepIdsForQuest.IsSubsetOf(completedStepsForAttempt))
        {
            attempt.Status = QuestAttemptStatus.Completed;
            attempt.CompletedAt = DateTimeOffset.UtcNow;
            await _attemptRepository.UpdateAsync(attempt, cancellationToken);
            _logger.LogInformation("All steps completed. Marked Quest Attempt {AttemptId} as 'Completed'.", attempt.Id);

            var parentQuest = await _questRepository.GetByIdAsync(attempt.QuestId, cancellationToken);
            if (parentQuest != null && parentQuest.Status != QuestStatus.Completed)
            {
                parentQuest.Status = QuestStatus.Completed;
                await _questRepository.UpdateAsync(parentQuest, cancellationToken);
                _logger.LogInformation("Parent Quest {QuestId} status updated to 'Completed'.", parentQuest.Id);
            }
        }
    }
}