using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("quest_submissions")]
public class QuestSubmission : BaseEntity
{
    [Column("attempt_id")]
    public Guid AttemptId { get; set; }

    // JSONB stored as string
    [Column("submission_data")]
    public string SubmissionData { get; set; } = string.Empty;

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