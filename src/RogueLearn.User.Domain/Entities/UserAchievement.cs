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
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

    [Column("context")]
    public string? Context { get; set; }
}