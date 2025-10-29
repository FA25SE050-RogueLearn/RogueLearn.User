using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("user_achievements")]
public class UserAchievement : BaseEntity
{
    [Column("auth_user_id")]
    public Guid AuthUserId { get; set; }

    [Column("achievement_id")]
    public Guid AchievementId { get; set; }

    [Column("earned_at")]
    public DateTimeOffset EarnedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("context")]
    public Dictionary<string, object>? Context { get; set; }
}