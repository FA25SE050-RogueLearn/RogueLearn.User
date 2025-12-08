using MediatR;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetQuestSkills;

public class GetQuestSkillsQuery : IRequest<GetQuestSkillsResponse?>
{
    public Guid QuestId { get; set; }
}
