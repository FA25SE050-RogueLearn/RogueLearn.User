using MediatR;

namespace RogueLearn.User.Application.Features.SkillDependencies.Commands.AddSkillDependency;

public sealed class AddSkillDependencyCommand : IRequest<AddSkillDependencyResponse>
{
    public Guid SkillId { get; set; }
    public Guid PrerequisiteSkillId { get; set; }
    public string? RelationshipType { get; set; }
}