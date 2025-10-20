using MediatR;

namespace RogueLearn.User.Application.Features.UserSkills.Commands.RemoveUserSkill;

public sealed class RemoveUserSkillCommand : IRequest
{
    public Guid AuthUserId { get; set; }
    public string SkillName { get; set; } = string.Empty;
}