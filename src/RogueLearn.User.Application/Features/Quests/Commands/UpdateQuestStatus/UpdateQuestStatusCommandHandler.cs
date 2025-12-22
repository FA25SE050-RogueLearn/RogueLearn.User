using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestStatus;

public class UpdateQuestStatusCommandHandler : IRequestHandler<UpdateQuestStatusCommand>
{
    private readonly IQuestRepository _questRepository;
    private readonly ILogger<UpdateQuestStatusCommandHandler> _logger;

    public UpdateQuestStatusCommandHandler(
        IQuestRepository questRepository,
        ILogger<UpdateQuestStatusCommandHandler> logger)
    {
        _questRepository = questRepository;
        _logger = logger;
    }

    public async Task Handle(UpdateQuestStatusCommand request, CancellationToken cancellationToken)
    {
        var quest = await _questRepository.GetByIdAsync(request.QuestId, cancellationToken);
        if (quest == null)
        {
            throw new NotFoundException("Quest", request.QuestId);
        }

        _logger.LogInformation("Updating status for Quest {QuestId} from {OldStatus} to {NewStatus}",
            quest.Id, quest.Status, request.NewStatus);

        quest.Status = request.NewStatus;
        quest.UpdatedAt = DateTimeOffset.UtcNow;

        await _questRepository.UpdateAsync(quest, cancellationToken);
    }
}