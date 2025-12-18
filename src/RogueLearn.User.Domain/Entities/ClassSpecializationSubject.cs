// RogueLearn.User/src/RogueLearn.User.Domain/Entities/ClassSpecializationSubject.cs
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

    // Removed PlaceholderSubjectCode - leveraging Subject relationship for codes

    [Column("semester")]
    public int Semester { get; set; }
}