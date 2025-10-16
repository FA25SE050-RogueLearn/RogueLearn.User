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

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}