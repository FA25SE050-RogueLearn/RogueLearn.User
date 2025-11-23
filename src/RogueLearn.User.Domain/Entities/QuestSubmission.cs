// RogueLearn.User/src/RogueLearn.User.Domain/Entities/QuestSubmission.cs
using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("quest_submissions")]
public class QuestSubmission : BaseEntity
{
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("quest_id")]
    public Guid QuestId { get; set; }

    [Column("step_id")]
    public Guid StepId { get; set; }

    [Column("activity_id")]
    public Guid ActivityId { get; set; }

    [Column("attempt_id")]
    public Guid AttemptId { get; set; }

    [Column("submission_data")]
    public string SubmissionData { get; set; } = string.Empty;

    [Column("submitted_at")]
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("graded_at")]
    public DateTimeOffset? GradedAt { get; set; }

    [Column("grade")]
    public decimal? Grade { get; set; }

    [Column("max_grade")]
    public decimal MaxGrade { get; set; }

    [Column("feedback")]
    public string? Feedback { get; set; }

    [Column("is_passed")]
    public bool? IsPassed { get; set; }

    [Column("attempt_number")]
    public int AttemptNumber { get; set; } = 1;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
