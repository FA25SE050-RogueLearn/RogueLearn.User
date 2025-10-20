using MediatR;

namespace RogueLearn.User.Application.Features.UserSkills.Commands.ResetUserSkillProgress;

public sealed class ResetUserSkillProgressCommand : IRequest
{
    public Guid AuthUserId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}