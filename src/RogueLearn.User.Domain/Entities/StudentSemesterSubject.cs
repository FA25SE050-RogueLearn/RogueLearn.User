using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Domain.Entities;

[Table("student_semester_subjects")]
public class StudentSemesterSubject : BaseEntity
{
    [Column("enrollment_id")]
    public Guid EnrollmentId { get; set; }

    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    [Column("academic_year")]
    public string AcademicYear { get; set; } = string.Empty;

    [Column("semester")]
    public int Semester { get; set; }

    [Column("status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public SubjectEnrollmentStatus Status { get; set; } = SubjectEnrollmentStatus.Enrolled;

    [Column("grade")]
    public string? Grade { get; set; }

    [Column("credits_earned")]
    public int CreditsEarned { get; set; } = 0;

    [Column("enrolled_at")]
    public DateTimeOffset EnrolledAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }
}