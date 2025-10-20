using MediatR;

namespace RogueLearn.User.Application.Features.SkillDependencies.Commands.RemoveSkillDependency;

public sealed class RemoveSkillDependencyCommand : IRequest
{
    public Guid SkillId { get; set; }
    public Guid PrerequisiteSkillId { get; set; }
}