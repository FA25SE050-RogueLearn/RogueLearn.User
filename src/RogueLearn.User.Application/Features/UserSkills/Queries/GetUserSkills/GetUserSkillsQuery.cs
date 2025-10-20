using MediatR;

namespace RogueLearn.User.Application.Features.UserSkills.Queries.GetUserSkills;

public sealed class GetUserSkillsQuery : IRequest<GetUserSkillsResponse>
{
    public Guid AuthUserId { get; set; }
}