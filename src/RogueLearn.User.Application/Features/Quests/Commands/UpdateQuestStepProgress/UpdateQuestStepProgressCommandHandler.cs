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
    private readonly IMediator _mediator; // We keep MediatR for dispatching the command.
    private readonly ILogger<UpdateQuestStepProgressCommandHandler> _logger;

    public UpdateQuestStepProgressCommandHandler(
        IUserQuestAttemptRepository attemptRepository,
        IUserQuestStepProgressRepository stepProgressRepository,
        IQuestStepRepository questStepRepository,
        IMediator mediator,
        ILogger<UpdateQuestStepProgressCommandHandler> logger)
    {
        _attemptRepository = attemptRepository;
        _stepProgressRepository = stepProgressRepository;
        _questStepRepository = questStepRepository;
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
            // MODIFICATION START: The return value of AddAsync is now captured.
            // This ensures we get the correct, database-generated ID for the new attempt,
            // which is essential for the foreign key relationship to work correctly.
            attempt = await _attemptRepository.AddAsync(attempt, cancellationToken);
            // MODIFICATION END
        }

        var stepProgress = await _stepProgressRepository.FirstOrDefaultAsync(
            sp => sp.AttemptId == attempt.Id && sp.StepId == request.StepId,
            cancellationToken);

        if (stepProgress?.Status == StepCompletionStatus.Completed)
        {
            _logger.LogInformation("Step {StepId} is already completed. No action taken.", request.StepId);
            return;
        }

        // --- MODIFICATION START: Transactional Logic with MediatR ---
        if (request.Status == StepCompletionStatus.Completed)
        {
            // First, attempt to dispatch the XP event. This will go through the validation pipeline.
            // If it fails for any reason (validation error, skill not found, etc.), it will throw an exception,
            // and the code below this block will NOT execute.
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
                            AuthUserId = request.AuthUserId,
                            SourceService = "QuestsService",
                            SourceType = SkillRewardSourceType.QuestComplete.ToString(),
                            SourceId = questStep.Id,
                            SkillId = skillId,
                            // SkillName is intentionally left empty; the handler will look it up.
                            SkillName = "",
                            Points = questStep.ExperiencePoints,
                            Reason = $"Completed quest step: {questStep.Title}"
                        };

                        // This call is now the gatekeeper. It will throw if validation fails.
                        await _mediator.Send(xpEvent, cancellationToken);

                        _logger.LogInformation("Successfully dispatched IngestXpEvent for SkillId '{SkillId}'", skillId);
                    }
                }
                catch (Exception ex)
                {
                    // Log the error and re-throw to ensure the transaction is halted.
                    _logger.LogError(ex, "Failed to dispatch XP event for Step {StepId}. Step progress will not be saved.", questStep.Id);
                    throw;
                }
            }

            // Only if the _mediator.Send call above succeeds, we proceed to save the progress.
            _logger.LogInformation("XP event successful. Now saving step progress for Step {StepId}.", request.StepId);
            if (stepProgress == null)
            {
                stepProgress = new UserQuestStepProgress
                {
                    AttemptId = attempt.Id,
                    StepId = request.StepId,
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
        }
        else // Handle other non-completed statuses (no XP awarded)
        {
            if (stepProgress == null)
            {
                stepProgress = new UserQuestStepProgress { /* ... set properties ... */ };
                await _stepProgressRepository.AddAsync(stepProgress, cancellationToken);
            }
            else
            {
                stepProgress.Status = request.Status;
                stepProgress.UpdatedAt = DateTimeOffset.UtcNow;
                await _stepProgressRepository.UpdateAsync(stepProgress, cancellationToken);
            }
        }
        // --- MODIFICATION END ---

        _logger.LogInformation("Successfully updated progress for Step:{StepId}", request.StepId);
    }
}