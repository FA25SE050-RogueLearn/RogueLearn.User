using MediatR;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetQuestStepContent;

public class GetQuestStepContentQuery : IRequest<QuestStepContentResponse>
{
    public Guid QuestStepId { get; set; }
}
