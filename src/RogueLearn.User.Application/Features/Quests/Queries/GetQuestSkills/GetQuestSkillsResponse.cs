namespace RogueLearn.User.Application.Features.Quests.Queries.GetQuestSkills;

public class GetQuestSkillsResponse
{
    public Guid QuestId { get; set; }
    public Guid? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public List<QuestSkillDto> Skills { get; set; } = new();
}

public class QuestSkillDto
{
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public decimal RelevanceWeight { get; set; }
    public List<PrerequisiteSkillDto> Prerequisites { get; set; } = new();
}

public class PrerequisiteSkillDto
{
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
}
