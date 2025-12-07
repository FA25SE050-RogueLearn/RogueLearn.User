using MediatR;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetQuestById;

public class GetQuestByIdQuery : IRequest<QuestDetailsDto>
{
    public Guid Id { get; set; }
    public Guid AuthUserId { get; set; }
}