using BuildingBlocks.Shared.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("guild_members")]
public class GuildMember : BaseEntity
{
    [Column("guild_id")]
    public Guid GuildId { get; set; }

    [Column("auth_user_id")]
    public Guid AuthUserId { get; set; }

    [Column("role")]
    [JsonConverter(typeof(StringEnumConverter))]
    public GuildRole Role { get; set; } = GuildRole.Member;

    [Column("status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public MemberStatus Status { get; set; } = MemberStatus.Active;

    [Column("joined_at")]
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("left_at")]
    public DateTimeOffset? LeftAt { get; set; }

    [Column("contribution_points")]
    public int ContributionPoints { get; set; } = 0;

    [Column("rank_within_guild")]
    public int? RankWithinGuild { get; set; }
}