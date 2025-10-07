using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("student_term_subjects")]
public class StudentTermSubject : BaseEntity
{
    [Column("enrollment_id")]
    public Guid EnrollmentId { get; set; }

    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    [Column("term_number")]
    public int TermNumber { get; set; }

    [Column("academic_year")]
    public string AcademicYear { get; set; } = string.Empty;

    [Column("semester")]
    public string Semester { get; set; } = string.Empty;

    [Column("status")]
    public SubjectEnrollmentStatus Status { get; set; } = SubjectEnrollmentStatus.Enrolled;

    [Column("grade")]
    public string? Grade { get; set; }

    [Column("credits_earned")]
    public int? CreditsEarned { get; set; } = 0;

    [Column("enrolled_at")]
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }
}