using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("party_invitations")]
public class PartyInvitation : BaseEntity
{
    [Column("party_id")]
    public Guid PartyId { get; set; }

    [Column("inviter_id")]
    public Guid InviterId { get; set; }

    [Column("invitee_id")]
    public Guid InviteeId { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [Column("status")]
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    [Column("message")]
    public string? Message { get; set; }

    [Column("invited_at")]
    public DateTimeOffset InvitedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("responded_at")]
    public DateTimeOffset? RespondedAt { get; set; }

    [Column("expires_at")]
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddDays(7);
}