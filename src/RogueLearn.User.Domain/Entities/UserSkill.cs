using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("user_skills")]
public class UserSkill : BaseEntity
{
    [Column("auth_user_id")]
    public Guid AuthUserId { get; set; }

    [Column("skill_id")]
    public Guid SkillId { get; set; }

    [Column("skill_name")]
    public string SkillName { get; set; } = string.Empty;

    [Column("experience_points")]
    public int ExperiencePoints { get; set; } = 0;

    [Column("level")]
    public int Level { get; set; } = 1;

    [Column("last_updated_at")]
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
