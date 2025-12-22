namespace RogueLearn.User.Application.Features.QuestProgress.Queries.GetUserProgressForQuest;

public class GetUserProgressForQuestResponse
{
    public Guid QuestId { get; set; }
    public string QuestStatus { get; set; } = "NotStarted";
    public Dictionary<Guid, string> StepStatuses { get; set; } = new();
}