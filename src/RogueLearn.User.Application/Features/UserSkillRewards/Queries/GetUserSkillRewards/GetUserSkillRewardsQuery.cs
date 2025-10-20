using MediatR;

namespace RogueLearn.User.Application.Features.UserSkillRewards.Queries.GetUserSkillRewards;

public sealed class GetUserSkillRewardsQuery : IRequest<GetUserSkillRewardsResponse>
{
    public Guid UserId { get; set; }
}