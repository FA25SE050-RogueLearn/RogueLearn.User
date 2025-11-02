// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/UpdateQuestStepProgress/UpdateQuestStepProgressCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestStepProgress;

public class UpdateQuestStepProgressCommandHandler : IRequestHandler<UpdateQuestStepProgressCommand>
{
    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly IUserQuestStepProgressRepository _stepProgressRepository;
    private readonly ILogger<UpdateQuestStepProgressCommandHandler> _logger;

    public UpdateQuestStepProgressCommandHandler(
        IUserQuestAttemptRepository attemptRepository,
        IUserQuestStepProgressRepository stepProgressRepository,
        ILogger<UpdateQuestStepProgressCommandHandler> logger)
    {
        _attemptRepository = attemptRepository;
        _stepProgressRepository = stepProgressRepository;
        _logger = logger;
    }

    public async Task Handle(UpdateQuestStepProgressCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating quest step progress for User:{AuthUserId}, Quest:{QuestId}, Step:{StepId} to Status:{Status}",
            request.AuthUserId, request.QuestId, request.StepId, request.Status);

        // 1. Find or create the UserQuestAttempt. This links all step progresses for a user's run of a quest.
        var attempt = await _attemptRepository.FirstOrDefaultAsync(
            a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
            cancellationToken);

        if (attempt == null)
        {
            _logger.LogInformation("No existing attempt found. Creating new UserQuestAttempt for User:{AuthUserId}, Quest:{QuestId}",
                request.AuthUserId, request.QuestId);
            attempt = new UserQuestAttempt
            {
                AuthUserId = request.AuthUserId,
                QuestId = request.QuestId,
                Status = QuestAttemptStatus.InProgress,
                StartedAt = DateTimeOffset.UtcNow
            };
            await _attemptRepository.AddAsync(attempt, cancellationToken);
        }

        // 2. Find or create the specific UserQuestStepProgress record.
        var stepProgress = await _stepProgressRepository.FirstOrDefaultAsync(
            sp => sp.AttemptId == attempt.Id && sp.StepId == request.StepId,
            cancellationToken);

        if (stepProgress == null)
        {
            _logger.LogInformation("Creating new UserQuestStepProgress for Step:{StepId}", request.StepId);
            stepProgress = new UserQuestStepProgress
            {
                AttemptId = attempt.Id,
                StepId = request.StepId,
                Status = request.Status,
                StartedAt = DateTimeOffset.UtcNow,
                CompletedAt = request.Status == StepCompletionStatus.Completed ? DateTimeOffset.UtcNow : null,
                AttemptsCount = 1
            };
            await _stepProgressRepository.AddAsync(stepProgress, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Updating existing UserQuestStepProgress for Step:{StepId}", request.StepId);
            stepProgress.Status = request.Status;
            stepProgress.CompletedAt = request.Status == StepCompletionStatus.Completed ? DateTimeOffset.UtcNow : stepProgress.CompletedAt;
            stepProgress.UpdatedAt = DateTimeOffset.UtcNow;
            stepProgress.AttemptsCount += 1;
            await _stepProgressRepository.UpdateAsync(stepProgress, cancellationToken);
        }

        _logger.LogInformation("Successfully updated progress for Step:{StepId}", request.StepId);
    }
}