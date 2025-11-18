// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/UpdateQuestStepProgress/UpdateQuestStepProgressCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;
using System.Text.Json;
using RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;

namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestStepProgress;

public class UpdateQuestStepProgressCommandHandler : IRequestHandler<UpdateQuestStepProgressCommand>
{
    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly IUserQuestStepProgressRepository _stepProgressRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<UpdateQuestStepProgressCommandHandler> _logger;

    // Helper record to deserialize an activity from the quest_step's content JSON
    private record ActivityPayload(JsonElement Payload);

    public UpdateQuestStepProgressCommandHandler(
        IUserQuestAttemptRepository attemptRepository,
        IUserQuestStepProgressRepository stepProgressRepository,
        IQuestStepRepository questStepRepository,
        IQuestRepository questRepository,
        IMediator mediator,
        ILogger<UpdateQuestStepProgressCommandHandler> logger)
    {
        _attemptRepository = attemptRepository;
        _stepProgressRepository = stepProgressRepository;
        _questStepRepository = questStepRepository;
        _questRepository = questRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(UpdateQuestStepProgressCommand request, CancellationToken cancellationToken)
    {
        // MODIFICATION: The 'StepId' in the request now refers to the weekly module (the QuestStep).
        // The 'ActivityId' is the specific task within that module being updated.
        _logger.LogInformation(
            "Updating activity progress for User:{AuthUserId}, Quest:{QuestId}, Module (Step):{StepId}, Activity:{ActivityId} to Status:{Status}",
            request.AuthUserId, request.QuestId, request.StepId, request.ActivityId, request.Status);

        // 1. Find the parent QuestStep (the weekly module)
        var questStep = await _questStepRepository.GetByIdAsync(request.StepId, cancellationToken);
        if (questStep is null || questStep.QuestId != request.QuestId)
        {
            throw new NotFoundException("QuestStep (Module)", request.StepId);
        }

        // 2. Find or create the UserQuestAttempt (tracks attempt on the overall quest)
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
            _logger.LogInformation("Created new UserQuestAttempt {AttemptId} for Quest {QuestId}", attempt.Id, request.QuestId);
        }

        // 3. Mark the parent Quest as InProgress if it's the first action
        var parentQuest = await _questRepository.GetByIdAsync(request.QuestId, cancellationToken)
            ?? throw new NotFoundException("Quest", request.QuestId);

        if (parentQuest.Status == QuestStatus.NotStarted)
        {
            parentQuest.Status = QuestStatus.InProgress;
            await _questRepository.UpdateAsync(parentQuest, cancellationToken);
            _logger.LogInformation("Parent Quest {QuestId} status updated to 'InProgress'.", parentQuest.Id);
        }

        // 4. Find or create the UserQuestStepProgress (tracks progress on the weekly module)
        var stepProgress = await _stepProgressRepository.FirstOrDefaultAsync(
            sp => sp.AttemptId == attempt.Id && sp.StepId == request.StepId,
            cancellationToken);

        if (stepProgress == null)
        {
            stepProgress = new UserQuestStepProgress
            {
                AttemptId = attempt.Id,
                StepId = request.StepId,
                Status = StepCompletionStatus.InProgress, // Start as InProgress
                StartedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                AttemptsCount = 1,
                CompletedActivityIds = Array.Empty<Guid>() // Initialize empty array
            };
            stepProgress = await _stepProgressRepository.AddAsync(stepProgress, cancellationToken);
        }

        // 5. Core Logic: Process the specific activity completion
        if (request.Status == StepCompletionStatus.Completed)
        {
            await CompleteActivityAndCheckForModuleCompletion(questStep, attempt, stepProgress, request.ActivityId, cancellationToken);
        }
        else
        {
            // Logic for other statuses (e.g., InProgress, Skipped) can be handled here if needed.
            // For now, we only focus on completion.
            stepProgress.Status = StepCompletionStatus.InProgress; // Ensure module is InProgress
            stepProgress.UpdatedAt = DateTimeOffset.UtcNow;
            await _stepProgressRepository.UpdateAsync(stepProgress, cancellationToken);
        }

        _logger.LogInformation("Successfully processed progress update for Activity:{ActivityId}", request.ActivityId);
    }

    private async Task CompleteActivityAndCheckForModuleCompletion(QuestStep questStep, UserQuestAttempt attempt, UserQuestStepProgress stepProgress, Guid activityId, CancellationToken cancellationToken)
    {
        // Ensure the activity isn't already completed to prevent duplicate XP awards.
        if (stepProgress.CompletedActivityIds?.Contains(activityId) == true)
        {
            _logger.LogInformation("Activity {ActivityId} is already completed for this step progress. No action taken.", activityId);
            return;
        }

        // Extract the specific activity and its payload from the module's content.
        var (activity, totalActivities) = FindActivityInContent(questStep.Content, activityId);

        if (activity == null)
        {
            _logger.LogWarning("Activity {ActivityId} not found within QuestStep {StepId}. Cannot process completion.", activityId, questStep.Id);
            throw new NotFoundException("Activity", activityId);
        }

        // Dispatch XP event for this specific activity
        if (activity.Payload.TryGetProperty("skillId", out var skillIdElement) &&
            Guid.TryParse(skillIdElement.GetString(), out var skillId) &&
            activity.Payload.TryGetProperty("experiencePoints", out var xpElement) &&
            xpElement.TryGetInt32(out var experiencePoints))
        {
            var xpEvent = new IngestXpEventCommand
            {
                AuthUserId = attempt.AuthUserId,
                SourceService = "QuestsService",
                SourceType = SkillRewardSourceType.QuestComplete.ToString(), // Or a new "ActivityComplete" type
                SourceId = activityId, // Idempotency key is now the activityId
                SkillId = skillId,
                SkillName = "", // Handler will look this up
                Points = experiencePoints,
                Reason = $"Completed activity in quest: {questStep.Title}"
            };

            await _mediator.Send(xpEvent, cancellationToken);
            _logger.LogInformation("Dispatched IngestXpEvent for Activity {ActivityId} with SkillId {SkillId}", activityId, skillId);
        }

        // Update the progress record by adding the completed activity's ID to the array.
        var completedIds = stepProgress.CompletedActivityIds?.ToList() ?? new List<Guid>();
        if (!completedIds.Contains(activityId))
        {
            completedIds.Add(activityId);
        }
        stepProgress.CompletedActivityIds = completedIds.ToArray();
        stepProgress.UpdatedAt = DateTimeOffset.UtcNow;

        // Check if all activities in the module are now complete.
        if (totalActivities > 0 && completedIds.Count >= totalActivities)
        {
            stepProgress.Status = StepCompletionStatus.Completed;
            stepProgress.CompletedAt = DateTimeOffset.UtcNow;
            _logger.LogInformation("All activities complete for Module (Step) {StepId}. Marking as 'Completed'.", questStep.Id);

            // After completing the module, check if the entire quest is now complete.
            await CheckForOverallQuestCompletion(attempt, cancellationToken);
        }
        else
        {
            stepProgress.Status = StepCompletionStatus.InProgress;
        }

        await _stepProgressRepository.UpdateAsync(stepProgress, cancellationToken);
    }

    private (ActivityPayload? activity, int totalCount) FindActivityInContent(object? content, Guid activityId)
    {
        if (content is not Dictionary<string, object> contentDict ||
            !contentDict.TryGetValue("activities", out var activitiesObj) ||
            activitiesObj is not List<object> activitiesList)
        {
            return (null, 0);
        }

        foreach (var activityObj in activitiesList)
        {
            if (activityObj is Dictionary<string, object> activityDict &&
                activityDict.TryGetValue("activityId", out var idObj) &&
                Guid.TryParse(idObj.ToString(), out var currentActivityId) &&
                currentActivityId == activityId)
            {
                // Found it. Now properly deserialize its payload.
                if (activityDict.TryGetValue("payload", out var payloadObj))
                {
                    // Reserialize and parse to get a JsonElement, which is what AiActivity expects
                    var payloadJson = JsonSerializer.Serialize(payloadObj);
                    var payloadElement = JsonDocument.Parse(payloadJson).RootElement;
                    return (new ActivityPayload(payloadElement), activitiesList.Count);
                }
            }
        }

        return (null, activitiesList.Count);
    }

    private async Task CheckForOverallQuestCompletion(UserQuestAttempt attempt, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking for overall quest completion for Quest {QuestId}", attempt.QuestId);

        var allStepsInQuest = (await _questStepRepository.FindByQuestIdAsync(attempt.QuestId, cancellationToken)).ToList();
        var totalStepsInQuest = allStepsInQuest.Count;

        var progressForQuestSteps = (await _stepProgressRepository.FindAsync(sp => sp.AttemptId == attempt.Id, cancellationToken)).ToList();
        var completedStepsInAttempt = progressForQuestSteps.Count(sp => sp.Status == StepCompletionStatus.Completed);

        _logger.LogInformation("Quest completion check: {CompletedSteps} of {TotalSteps} modules (steps) are complete for Attempt {AttemptId}",
            completedStepsInAttempt, totalStepsInQuest, attempt.Id);

        if (totalStepsInQuest > 0 && completedStepsInAttempt >= totalStepsInQuest)
        {
            if (attempt.Status != QuestAttemptStatus.Completed)
            {
                attempt.Status = QuestAttemptStatus.Completed;
                attempt.CompletedAt = DateTimeOffset.UtcNow;
                await _attemptRepository.UpdateAsync(attempt, cancellationToken);
                _logger.LogInformation("All modules completed. Marked Quest Attempt {AttemptId} as 'Completed'.", attempt.Id);

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
}