using BuildingBlocks.Shared.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("guild_posts")]
public class GuildPost : BaseEntity
{
    [Column("guild_id")]
    public Guid GuildId { get; set; }

    [Column("author_id")]
    public Guid AuthorId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("post_type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public PostType PostType { get; set; } = PostType.general;

    [Column("is_pinned")]
    public bool IsPinned { get; set; } = false;

    // New: moderation status for post lifecycle
    [Column("status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public GuildPostStatus Status { get; set; } = GuildPostStatus.published;

    // New: lock flag to prevent edits/deletes by authors
    [Column("is_locked")]
    public bool IsLocked { get; set; } = false;

    [Column("is_announcement")]
    public bool IsAnnouncement { get; set; } = false;

    [Column("attachments")]
    public Dictionary<string, object>? Attachments { get; set; }

    // New: tags for filtering and organization
    [Column("tags")]
    public string[]? Tags { get; set; }

    [Column("like_count")]
    public int LikeCount { get; set; } = 0;

    [Column("comment_count")]
    public int CommentCount { get; set; } = 0;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("deleted_at")]
    public DateTimeOffset? DeletedAt { get; set; }
}