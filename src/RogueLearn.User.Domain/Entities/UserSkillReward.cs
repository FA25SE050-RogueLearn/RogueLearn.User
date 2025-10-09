using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("user_skill_rewards")]
public class UserSkillReward : BaseEntity
{
    [Column("auth_user_id")]
    public Guid AuthUserId { get; set; }

    [Column("source_service")]
    public string SourceService { get; set; } = string.Empty;

    [Column("source_type")]
    public string SourceType { get; set; } = string.Empty;

    [Column("source_id")]
    public Guid? SourceId { get; set; }

    [Column("skill_name")]
    public string SkillName { get; set; } = string.Empty;

    [Column("points_awarded")]
    public int PointsAwarded { get; set; }

    [Column("reason")]
    public string? Reason { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}