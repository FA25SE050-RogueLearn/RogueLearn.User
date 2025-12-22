using MediatR;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestStatus;

public class UpdateQuestStatusCommand : IRequest
{
    public Guid QuestId { get; set; }
    public QuestStatus NewStatus { get; set; }
}