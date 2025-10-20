namespace RogueLearn.User.Application.Features.UserSkillRewards.Queries.GetUserSkillRewards;

public sealed class GetUserSkillRewardsResponse
{
    public List<UserSkillRewardDto> Rewards { get; set; } = new();
}