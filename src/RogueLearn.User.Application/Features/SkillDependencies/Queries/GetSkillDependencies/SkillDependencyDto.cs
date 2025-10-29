namespace RogueLearn.User.Application.Features.SkillDependencies.Queries.GetSkillDependencies;
using RogueLearn.User.Domain.Enums;

public sealed class SkillDependencyDto
{
    public Guid Id { get; set; }
    public Guid SkillId { get; set; }
    public Guid PrerequisiteSkillId { get; set; }
    public SkillRelationshipType RelationshipType { get; set; } = SkillRelationshipType.Prerequisite;
    public DateTimeOffset CreatedAt { get; set; }
}