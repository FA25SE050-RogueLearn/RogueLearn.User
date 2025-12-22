namespace RogueLearn.User.Application.Features.SubjectSkillMappings.Queries.GetSubjectSkillMappings;

public class SubjectSkillMappingDto
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public decimal RelevanceWeight { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // NEW: List of prerequisites for this skill to build the tree structure
    public List<PrerequisiteDto> Prerequisites { get; set; } = new();
}

public class PrerequisiteDto
{
    public Guid PrerequisiteSkillId { get; set; }
    public string PrerequisiteSkillName { get; set; } = string.Empty;
}