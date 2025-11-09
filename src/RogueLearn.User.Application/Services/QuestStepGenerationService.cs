using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;

namespace RogueLearn.User.Application.Services;

public interface IQuestStepGenerationService
{
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background job failed: Error generating steps for Quest {QuestId}", questId);
            throw; // Hangfire will retry
        }
    }
}