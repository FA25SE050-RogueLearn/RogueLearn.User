using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("match_player_summaries")]
public class MatchPlayerSummary : BaseEntity
{
    [Column("match_result_id")]
    public Guid MatchResultId { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("client_id")]
    public long? ClientId { get; set; }

    [Column("total_questions")]
    public int TotalQuestions { get; set; }

    [Column("correct_answers")]
    public int CorrectAnswers { get; set; }

    [Column("average_time")]
    public double? AverageTime { get; set; }

    [Column("topic_breakdown")]
    public string? TopicBreakdownJson { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
