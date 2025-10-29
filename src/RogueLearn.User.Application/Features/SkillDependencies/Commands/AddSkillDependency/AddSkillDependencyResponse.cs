namespace RogueLearn.User.Application.Features.SkillDependencies.Commands.AddSkillDependency;
using RogueLearn.User.Domain.Enums;

public sealed class AddSkillDependencyResponse
{
    public Guid Id { get; set; }
    public Guid SkillId { get; set; }
    public Guid PrerequisiteSkillId { get; set; }
    public SkillRelationshipType RelationshipType { get; set; } = SkillRelationshipType.Prerequisite;
    public DateTimeOffset CreatedAt { get; set; }
}