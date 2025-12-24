using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Domain.Entities;

[Table("user_skill_rewards")]
public class UserSkillReward : BaseEntity
{
    [Column("auth_user_id")]
    public Guid AuthUserId { get; set; }

    // Renamed column in DB from source_type -> source_service
    // The C# property 'SourceService' now holds the ENUM value.
    // The original string 'source_service' column is gone.
    [Column("source_service")]
    [JsonConverter(typeof(StringEnumConverter))]
    public SkillRewardSourceType SourceService { get; set; }

    [Column("source_id")]
    public Guid? SourceId { get; set; }

    [Column("skill_id")]
    public Guid SkillId { get; set; }

    [Column("points_awarded")]
    public int PointsAwarded { get; set; }

    [Column("reason")]
    public string? Reason { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}