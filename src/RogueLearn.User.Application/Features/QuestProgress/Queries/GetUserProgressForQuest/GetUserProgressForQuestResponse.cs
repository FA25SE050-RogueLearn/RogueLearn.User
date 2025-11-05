// RogueLearn.User/src/RogueLearn.User.Application/Features/QuestProgress/Queries/GetUserProgressForQuest/GetUserProgressForQuestResponse.cs
// MODIFICATION: Namespace updated.
namespace RogueLearn.User.Application.Features.QuestProgress.Queries.GetUserProgressForQuest;

// MODIFICATION: Class name updated.
public class GetUserProgressForQuestResponse
{
    public Guid QuestId { get; set; }
    public string QuestStatus { get; set; } = "NotStarted";
    public Dictionary<Guid, string> StepStatuses { get; set; } = new();
}