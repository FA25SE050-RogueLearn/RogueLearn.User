using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("game_sessions")]
public class GameSession : BaseEntity
{
    [Column("session_id")]
    public Guid SessionId { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("relay_join_code")]
    public string? RelayJoinCode { get; set; }

    [Column("pack_id")]
    public string? PackId { get; set; }

    [Column("subject")]
    public string? Subject { get; set; }

    [Column("topic")]
    public string? Topic { get; set; }

    [Column("difficulty")]
    public string? Difficulty { get; set; }

    [Column("question_pack")]
    public string? QuestionPackJson { get; set; }

    [Column("status")]
    public string Status { get; set; } = "created";

    [Column("match_result_id")]
    public Guid? MatchResultId { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }
}
