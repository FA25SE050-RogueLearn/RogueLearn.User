using MediatR;

namespace RogueLearn.User.Application.Features.UserSkills.Commands.AddUserSkill;

public sealed class AddUserSkillCommand : IRequest<AddUserSkillResponse>
{
    public Guid AuthUserId { get; set; }
    public string SkillName { get; set; } = string.Empty;
}