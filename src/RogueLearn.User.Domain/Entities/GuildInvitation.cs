using System.Text.Json.Serialization;
using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("guild_invitations")]
public class GuildInvitation : BaseEntity
{
    [Column("guild_id")]
    public Guid GuildId { get; set; }

    [Column("inviter_id")]
    public Guid? InviterId { get; set; }

    [Column("invitee_id")]
    public Guid InviteeId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    [Column("invitation_type")]
    public InvitationType InvitationType { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    [Column("status")]
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    [Column("message")]
    public string? Message { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("responded_at")]
    public DateTimeOffset? RespondedAt { get; set; }

    [Column("expires_at")]
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddDays(14);
}