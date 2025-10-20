namespace RogueLearn.User.Application.Features.SkillDependencies.Queries.GetSkillDependencies;

public sealed class SkillDependencyDto
{
    public Guid Id { get; set; }
    public Guid SkillId { get; set; }
    public Guid PrerequisiteSkillId { get; set; }
    public string RelationshipType { get; set; } = "Prerequisite";
    public DateTimeOffset CreatedAt { get; set; }
}