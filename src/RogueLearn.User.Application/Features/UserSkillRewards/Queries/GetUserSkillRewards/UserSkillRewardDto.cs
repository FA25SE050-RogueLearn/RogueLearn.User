namespace RogueLearn.User.Application.Features.UserSkillRewards.Queries.GetUserSkillRewards;

public sealed class UserSkillRewardDto
{
    public Guid Id { get; set; }
    public string SourceService { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public Guid? SourceId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int PointsAwarded { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}