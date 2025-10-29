using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("quest_analytics")]
public class QuestAnalytics : BaseEntity
{
    [Column("quest_id")]
    public Guid QuestId { get; set; }

    [Column("date_recorded")]
    public DateOnly DateRecorded { get; set; }

    [Column("total_attempts")]
    public int TotalAttempts { get; set; } = 0;

    [Column("successful_completions")]
    public int SuccessfulCompletions { get; set; } = 0;

    [Column("average_completion_time_minutes")]
    public decimal? AverageCompletionTimeMinutes { get; set; }

    [Column("average_attempts_to_complete")]
    public decimal? AverageAttemptsToComplete { get; set; }

    [Column("abandonment_rate")]
    public decimal? AbandonmentRate { get; set; }

    [Column("difficulty_rating")]
    public decimal? DifficultyRating { get; set; }

    [Column("engagement_score")]
    public decimal? EngagementScore { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}