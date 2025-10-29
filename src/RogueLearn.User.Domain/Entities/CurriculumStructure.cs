using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("curriculum_structure")]
public class CurriculumStructure : BaseEntity
{
    [Column("curriculum_version_id")]
    public Guid CurriculumVersionId { get; set; }

    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    [Column("semester")]
    public int Semester { get; set; }

    [Column("is_mandatory")]
    public bool IsMandatory { get; set; } = true;

    [Column("prerequisite_subject_ids")]
    public Guid[]? PrerequisiteSubjectIds { get; set; }

    [Column("prerequisites_text")]
    public string? PrerequisitesText { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}