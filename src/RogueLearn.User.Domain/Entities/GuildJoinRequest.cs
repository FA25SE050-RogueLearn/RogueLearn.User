using BuildingBlocks.Shared.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("guild_join_requests")]
public class GuildJoinRequest : BaseEntity
{
    [Column("guild_id")]
    public Guid GuildId { get; set; }

    [Column("requester_id")]
    public Guid RequesterId { get; set; }

    [Column("status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public GuildJoinRequestStatus Status { get; set; } = GuildJoinRequestStatus.Pending;

    [Column("message")]
    public string? Message { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("responded_at")]
    public DateTimeOffset? RespondedAt { get; set; }

    [Column("expires_at")]
    public DateTimeOffset? ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddDays(14);
}