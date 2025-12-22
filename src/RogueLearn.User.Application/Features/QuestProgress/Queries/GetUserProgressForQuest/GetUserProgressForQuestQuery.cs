using MediatR;

namespace RogueLearn.User.Application.Features.QuestProgress.Queries.GetUserProgressForQuest;

public class GetUserProgressForQuestQuery : IRequest<GetUserProgressForQuestResponse?>
{
    public Guid AuthUserId { get; set; }
    public Guid QuestId { get; set; }
}