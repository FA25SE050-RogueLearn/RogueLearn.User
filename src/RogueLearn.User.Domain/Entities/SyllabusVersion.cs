using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("syllabus_versions")]
public class SyllabusVersion : BaseEntity
{
    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    [Column("version_number")]
    public int VersionNumber { get; set; }

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("effective_date")]
    public DateOnly EffectiveDate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}