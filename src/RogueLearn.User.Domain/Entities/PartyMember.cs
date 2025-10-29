using System.Text.Json.Serialization;
using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("party_members")]
public class PartyMember : BaseEntity
{
    [Column("party_id")]
    public Guid PartyId { get; set; }

    [Column("auth_user_id")]
    public Guid AuthUserId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    [Column("role")]
    public PartyRole Role { get; set; } = PartyRole.Member;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    [Column("status")]
    public MemberStatus Status { get; set; } = MemberStatus.Active;

    [Column("joined_at")]
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("left_at")]
    public DateTimeOffset? LeftAt { get; set; }

    [Column("contribution_score")]
    public int ContributionScore { get; set; } = 0;
}