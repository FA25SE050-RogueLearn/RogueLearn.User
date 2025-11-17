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
        _logger.LogInformation(
            "Updating quest step progress for User:{AuthUserId}, Quest:{QuestId}, Step:{StepId} to Status:{Status}",
            request.AuthUserId, request.QuestId, request.StepId, request.Status);

        var questStep = await _questStepRepository.GetByIdAsync(request.StepId, cancellationToken);
        if (questStep is null || questStep.QuestId != request.QuestId)
        {
            throw new NotFoundException("QuestStep", request.StepId);
        }

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

        var parentQuest = await _questRepository.GetByIdAsync(request.QuestId, cancellationToken);
        if (parentQuest is null)
        {
            throw new NotFoundException("Quest", request.QuestId);
        }

        if (parentQuest.Status == QuestStatus.NotStarted)
        {
            parentQuest.Status = QuestStatus.InProgress;
            await _questRepository.UpdateAsync(parentQuest, cancellationToken);
            _logger.LogInformation("Parent Quest {QuestId} status updated to 'InProgress' due to first user action.", parentQuest.Id);
        }

        var stepProgress = await _stepProgressRepository.FirstOrDefaultAsync(
            sp => sp.AttemptId == attempt.Id && sp.StepId == request.StepId,
            cancellationToken);

        if (stepProgress?.Status == StepCompletionStatus.Completed && request.Status == StepCompletionStatus.Completed)
        {
            _logger.LogInformation("Step {StepId} is already completed. No action taken.", request.StepId);
            return;
        }

        if (request.Status == StepCompletionStatus.Completed)
        {
            await CompleteStepAndCheckForQuestCompletion(questStep, attempt, stepProgress, cancellationToken);
        }
        else
        {
            if (stepProgress == null)
            {
                stepProgress = new UserQuestStepProgress
                {
                    AttemptId = attempt.Id,
                    StepId = request.StepId,
                    Status = request.Status,
                    StartedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    AttemptsCount = 1
                };
                await _stepProgressRepository.AddAsync(stepProgress, cancellationToken);
            }
            else
            {
                stepProgress.Status = request.Status;
                stepProgress.UpdatedAt = DateTimeOffset.UtcNow;
                await _stepProgressRepository.UpdateAsync(stepProgress, cancellationToken);
            }
        }

        _logger.LogInformation("Successfully updated progress for Step:{StepId} to Status:{Status}", request.StepId, request.Status);
    }

    private async Task CompleteStepAndCheckForQuestCompletion(QuestStep questStep, UserQuestAttempt attempt, UserQuestStepProgress? stepProgress, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(questStep.Content))
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(questStep.Content);
                if (jsonDoc.RootElement.TryGetProperty("skillId", out var skillIdElement) &&
                    skillIdElement.ValueKind == JsonValueKind.String &&
                    Guid.TryParse(skillIdElement.GetString(), out var skillId))
                {
                    var xpEvent = new IngestXpEventCommand
                    {
                        AuthUserId = attempt.AuthUserId,
                        SourceService = "QuestsService",
                        SourceType = SkillRewardSourceType.QuestComplete.ToString(),
                        SourceId = questStep.Id,
                        SkillId = skillId,
                        SkillName = "",
                        Points = questStep.ExperiencePoints,
                        Reason = $"Completed quest step: {questStep.Title}"
                    };

                    await _mediator.Send(xpEvent, cancellationToken);
                    _logger.LogInformation("Successfully dispatched IngestXpEvent for SkillId '{SkillId}'", skillId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch XP event for Step {StepId}. Step progress will not be saved.", questStep.Id);
                throw;
            }
        }

        _logger.LogInformation("XP event successful. Now saving step progress for Step {StepId}.", questStep.Id);
        if (stepProgress == null)
        {
            stepProgress = new UserQuestStepProgress
            {
                AttemptId = attempt.Id,
                StepId = questStep.Id,
                Status = StepCompletionStatus.Completed,
                StartedAt = DateTimeOffset.UtcNow,
                CompletedAt = DateTimeOffset.UtcNow,
                AttemptsCount = 1
            };
            await _stepProgressRepository.AddAsync(stepProgress, cancellationToken);
        }
        else
        {
            stepProgress.Status = StepCompletionStatus.Completed;
            stepProgress.CompletedAt = DateTimeOffset.UtcNow;
            stepProgress.UpdatedAt = DateTimeOffset.UtcNow;
            stepProgress.AttemptsCount += 1;
            await _stepProgressRepository.UpdateAsync(stepProgress, cancellationToken);
        }

        _logger.LogInformation("Checking for overall quest completion for Quest {QuestId}", attempt.QuestId);

        var totalStepsInQuest = (await _questStepRepository.FindByQuestIdAsync(attempt.QuestId, cancellationToken)).Count();

        // MODIFICATION: Replaced the faulty FindAsync call with our new, reliable method.
        var completedStepsInAttempt = await _stepProgressRepository.GetCompletedStepsCountForAttemptAsync(attempt.Id, cancellationToken);

        _logger.LogInformation("Quest completion check: {CompletedSteps} of {TotalSteps} steps are complete for Attempt {AttemptId}",
            completedStepsInAttempt, totalStepsInQuest, attempt.Id);

        if (totalStepsInQuest > 0 && completedStepsInAttempt >= totalStepsInQuest)
        {
            if (attempt.Status != QuestAttemptStatus.Completed)
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
}