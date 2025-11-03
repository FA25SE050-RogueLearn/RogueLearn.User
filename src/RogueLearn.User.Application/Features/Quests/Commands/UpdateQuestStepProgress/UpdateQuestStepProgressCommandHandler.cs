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
    private readonly IQuestStepRepository _questStepRepository; // ADDED: To fetch the step details.
    private readonly IMediator _mediator; // ADDED: To dispatch the XP event.
    private readonly ILogger<UpdateQuestStepProgressCommandHandler> _logger;

    public UpdateQuestStepProgressCommandHandler(
        IUserQuestAttemptRepository attemptRepository,
        IUserQuestStepProgressRepository stepProgressRepository,
        IQuestStepRepository questStepRepository, // ADDED
        IMediator mediator, // ADDED
        ILogger<UpdateQuestStepProgressCommandHandler> logger)
    {
        _attemptRepository = attemptRepository;
        _stepProgressRepository = stepProgressRepository;
        _questStepRepository = questStepRepository; // ADDED
        _mediator = mediator; // ADDED
        _logger = logger;
    }

    public async Task Handle(UpdateQuestStepProgressCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating quest step progress for User:{AuthUserId}, Quest:{QuestId}, Step:{StepId} to Status:{Status}",
            request.AuthUserId, request.QuestId, request.StepId, request.Status);

        // MODIFICATION: Fetch the QuestStep to access its content and XP value.
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
            // Avoid re-awarding XP or changing completion date if already completed.
            if (stepProgress.Status == StepCompletionStatus.Completed)
            {
                _logger.LogInformation("Step {StepId} is already completed. No action taken.", request.StepId);
                return;
            }

            _logger.LogInformation("Updating existing UserQuestStepProgress for Step:{StepId}", request.StepId);
            stepProgress.Status = request.Status;
            stepProgress.CompletedAt = request.Status == StepCompletionStatus.Completed ? DateTimeOffset.UtcNow : stepProgress.CompletedAt;
            stepProgress.UpdatedAt = DateTimeOffset.UtcNow;
            stepProgress.AttemptsCount += 1;
            await _stepProgressRepository.UpdateAsync(stepProgress, cancellationToken);
        }

        // MODIFICATION: Award skill XP if the step is being marked as completed.
        if (request.Status == StepCompletionStatus.Completed && !string.IsNullOrWhiteSpace(questStep.Content))
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(questStep.Content);
                if (jsonDoc.RootElement.TryGetProperty("skillTag", out var skillTagElement) &&
                    skillTagElement.ValueKind == JsonValueKind.String)
                {
                    var skillName = skillTagElement.GetString();
                    if (!string.IsNullOrWhiteSpace(skillName))
                    {
                        var xpEvent = new IngestXpEventCommand
                        {
                            AuthUserId = request.AuthUserId,
                            SourceService = "QuestsService",
                            SourceType = SkillRewardSourceType.QuestComplete.ToString(),
                            SourceId = questStep.Id,
                            SkillName = skillName,
                            Points = questStep.ExperiencePoints,
                            Reason = $"Completed quest step: {questStep.Title}"
                        };
                        await _mediator.Send(xpEvent, cancellationToken);
                        _logger.LogInformation("Dispatched IngestXpEvent for Skill '{SkillName}' with {Points} XP for completing Step {StepId}",
                            skillName, questStep.ExperiencePoints, request.StepId);
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Could not parse skillTag from content for QuestStep {StepId}", request.StepId);
            }
        }

        _logger.LogInformation("Successfully updated progress for Step:{StepId}", request.StepId);
    }
}