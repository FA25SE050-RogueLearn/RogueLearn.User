using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("parties")]
public class Party : BaseEntity
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [Column("party_type")]
    public PartyType PartyType { get; set; }

    [Column("max_members")]
    public int MaxMembers { get; set; } = 6;

    [Column("current_member_count")]
    public int CurrentMemberCount { get; set; } = 1;

    [Column("is_public")]
    public bool IsPublic { get; set; } = true;

    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}