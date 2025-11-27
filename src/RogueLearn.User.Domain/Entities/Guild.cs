using BuildingBlocks.Shared.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("guilds")]
public class Guild : BaseEntity
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("guild_type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public GuildType GuildType { get; set; }

    [Column("max_members")]
    public int MaxMembers { get; set; } = 100;

    [Column("current_member_count")]
    public int CurrentMemberCount { get; set; } = 1;

    [Column("merit_points")]
    public int MeritPoints { get; set; } = 0;

    [Column("is_public")]
    public bool IsPublic { get; set; } = true;

    [Column("is_lecturer_guild")]
    public bool IsLecturerGuild { get; set; } = false;

    [Column("requires_approval")]
    public bool RequiresApproval { get; set; } = false;

    [Column("banner_image_url")]
    public string? BannerImageUrl { get; set; }

    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}