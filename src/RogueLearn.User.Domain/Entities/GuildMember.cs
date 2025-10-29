using System.Text.Json.Serialization;
using BuildingBlocks.Shared.Common;
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

    [JsonConverter(typeof(JsonStringEnumConverter))]
    [Column("role")]
    public GuildRole Role { get; set; } = GuildRole.Member;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    [Column("status")]
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