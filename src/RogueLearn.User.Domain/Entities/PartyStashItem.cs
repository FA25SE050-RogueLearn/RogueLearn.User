using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("party_stash_items")]
public class PartyStashItem : BaseEntity
{
    [Column("party_id")]
    public Guid PartyId { get; set; }

    [Column("original_note_id")]
    public Guid OriginalNoteId { get; set; }

    [Column("shared_by_user_id")]
    public Guid SharedByUserId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("content")]
    public object Content { get; set; } = new();

    [Column("tags")]
    public string[]? Tags { get; set; }

    [Column("shared_at")]
    public DateTimeOffset SharedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}