// RogueLearn.User/src/RogueLearn.User.Application/Services/QuestStepGenerationService.cs
using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;

namespace RogueLearn.User.Application.Services;

public interface IQuestStepGenerationService
{
    // Added AutomaticRetry attribute to give Hangfire more control over retries.
    [AutomaticRetry(Attempts = 5, DelaysInSeconds = new[] { 20, 40, 60, 120, 300 })]
    Task GenerateQuestStepsAsync(Guid authUserId, Guid questId);
}

public class QuestStepGenerationService : IQuestStepGenerationService
{
    private readonly IMediator _mediator;
    private readonly ILogger<QuestStepGenerationService> _logger;

    public QuestStepGenerationService(
        IMediator mediator,
        ILogger<QuestStepGenerationService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task GenerateQuestStepsAsync(Guid authUserId, Guid questId)
    {
        try
        {
            _logger.LogInformation("Background job started: Generating steps for Quest {QuestId}", questId);

            await _mediator.Send(new GenerateQuestStepsCommand
            {
                AuthUserId = authUserId,
                QuestId = questId
            });

            _logger.LogInformation("Background job completed: Steps generated for Quest {QuestId}", questId);
        }
        // ARCHITECTURAL FIX: Catch the specific NotFoundException caused by the race condition.
        catch (NotFoundException ex)
        {
            // By catching and re-throwing, we allow Hangfire's AutomaticRetry mechanism to handle
            // this transient error. The job will be retried after a delay, by which time the
            // quest record will be available in the database.
            _logger.LogWarning(ex, "Background job failed for Quest {QuestId} because the entity was not found. This may be a transient issue. The job will be retried by Hangfire.", questId);
            throw; // Re-throw to trigger Hangfire's retry logic.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background job failed with an unhandled exception: Error generating steps for Quest {QuestId}", questId);
            throw; // Re-throw for other unexpected errors.
        }
    }
}