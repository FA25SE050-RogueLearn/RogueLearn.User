using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("student_enrollments")]
public class StudentEnrollment : BaseEntity
{
    [Column("auth_user_id")]
    public Guid AuthUserId { get; set; }

    [Column("curriculum_version_id")]
    public Guid CurriculumVersionId { get; set; }

    [Column("enrollment_date")]
    public DateTime EnrollmentDate { get; set; }

    [Column("expected_graduation_date")]
    public DateTime? ExpectedGraduationDate { get; set; }

    [Column("status")]
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
}