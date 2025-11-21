using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("class_specialization_subjects")]
public class ClassSpecializationSubject : BaseEntity
{
    [Column("class_id")]
    public Guid ClassId { get; set; }

    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    // --- ADD THESE NEW PROPERTIES ---
    [Column("placeholder_subject_code")]
    public string PlaceholderSubjectCode { get; set; } = string.Empty;

    [Column("semester")]
    public int Semester { get; set; }
    // --- END OF ADDITION ---
}