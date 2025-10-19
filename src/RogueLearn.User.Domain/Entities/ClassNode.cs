using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("class_nodes")]
public class ClassNode : BaseEntity
{
    [Column("class_id")]
    public Guid ClassId { get; set; }

    [Column("parent_id")]
    public Guid? ParentId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("node_type")]
    public string? NodeType { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("sequence")]
    public int Sequence { get; set; } = 0;

    // NEW: Soft delete flag for nodes
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // NEW: Protect AI-imported structure unless explicitly unlocked
    [Column("is_locked_by_import")]
    public bool IsLockedByImport { get; set; } = false;

    // NEW: Optional metadata for traceability (source, external_path_hash, difficulty, etc.)
    [Column("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}