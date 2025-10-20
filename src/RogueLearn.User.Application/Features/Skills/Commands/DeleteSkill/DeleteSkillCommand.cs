using MediatR;

namespace RogueLearn.User.Application.Features.Skills.Commands.DeleteSkill;

public sealed class DeleteSkillCommand : IRequest
{
    public Guid Id { get; set; }
}