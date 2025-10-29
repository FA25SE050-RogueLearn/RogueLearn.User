using MediatR;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.SkillDependencies.Commands.AddSkillDependency;

public sealed class AddSkillDependencyCommand : IRequest<AddSkillDependencyResponse>
{
    public Guid SkillId { get; set; }
    public Guid PrerequisiteSkillId { get; set; }
    public SkillRelationshipType? RelationshipType { get; set; }
}