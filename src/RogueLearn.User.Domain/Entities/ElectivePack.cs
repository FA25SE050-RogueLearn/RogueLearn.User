using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("elective_packs")]
public class ElectivePack : BaseEntity
{
    [Column("version")]
    public string Version { get; set; } = string.Empty;

    [Column("source_type")]
    public string SourceType { get; set; } = string.Empty;

    [Column("subject_id")]
    public Guid? SubjectId { get; set; }

    [Column("curriculum_version_id")]
    public Guid? CurriculumVersionId { get; set; }

    [Column("metadata")]
    public string? Metadata { get; set; }

    [Column("approved_by")]
    public Guid? ApprovedBy { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }
}