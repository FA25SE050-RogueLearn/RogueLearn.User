namespace RogueLearn.User.Application.Features.SkillDependencies.Commands.AddSkillDependency;

public sealed class AddSkillDependencyResponse
{
    public Guid Id { get; set; }
    public Guid SkillId { get; set; }
    public Guid PrerequisiteSkillId { get; set; }
    public string RelationshipType { get; set; } = "Prerequisite";
    public DateTimeOffset CreatedAt { get; set; }
}