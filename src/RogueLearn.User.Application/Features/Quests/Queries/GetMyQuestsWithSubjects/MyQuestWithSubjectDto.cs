namespace RogueLearn.User.Application.Features.Quests.Queries.GetMyQuestsWithSubjects;

public class MyQuestWithSubjectDto
{
    public Guid QuestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? SubjectId { get; set; }
    public string? SubjectCode { get; set; }
    public string? SubjectName { get; set; }
    public int? Credits { get; set; }
}

