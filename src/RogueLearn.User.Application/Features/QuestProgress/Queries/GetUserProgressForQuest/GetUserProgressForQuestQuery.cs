// RogueLearn.User/src/RogueLearn.User.Application/Features/QuestProgress/Queries/GetUserProgressForQuest/GetUserProgressForQuestQuery.cs
using MediatR;

// MODIFICATION: Namespace updated.
namespace RogueLearn.User.Application.Features.QuestProgress.Queries.GetUserProgressForQuest;

// MODIFICATION: Class name updated.
public class GetUserProgressForQuestQuery : IRequest<GetUserProgressForQuestResponse?>
{
    public Guid AuthUserId { get; set; }
    public Guid QuestId { get; set; }
}