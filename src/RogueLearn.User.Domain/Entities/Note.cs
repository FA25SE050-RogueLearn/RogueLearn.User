using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("notes")]
public class Note : BaseEntity
{
    [Column("auth_user_id")]
    public Guid AuthUserId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    // Stored as JSONB in database; represented as string in the entity for simplicity
    [Column("content")]
    public string? Content { get; set; }

    [Column("is_public")]
    public bool IsPublic { get; set; } = false;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
