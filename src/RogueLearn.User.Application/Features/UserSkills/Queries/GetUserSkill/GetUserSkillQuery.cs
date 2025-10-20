using MediatR;

namespace RogueLearn.User.Application.Features.UserSkills.Queries.GetUserSkill;

public sealed class GetUserSkillQuery : IRequest<GetUserSkillResponse>
{
    public Guid AuthUserId { get; set; }
    public string SkillName { get; set; } = string.Empty;
}