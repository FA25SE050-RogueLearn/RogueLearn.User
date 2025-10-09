using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("subjects")]
public class Subject : BaseEntity
{
    [Column("subject_code")]
    public string SubjectCode { get; set; } = string.Empty;

    [Column("subject_name")]
    public string SubjectName { get; set; } = string.Empty;

    [Column("credits")]
    public int Credits { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}