// RogueLearn.User/src/RogueLearn.User.Domain/Entities/StudentEnrollment.cs
using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Domain.Entities;

[Table("student_enrollments")]
public class StudentEnrollment : BaseEntity
{
    [Column("auth_user_id")]
    public Guid AuthUserId { get; set; }

    [Column("enrollment_date")]
    public DateOnly EnrollmentDate { get; set; }

    [Column("expected_graduation_date")]
    public DateOnly? ExpectedGraduationDate { get; set; }

    [Column("status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}