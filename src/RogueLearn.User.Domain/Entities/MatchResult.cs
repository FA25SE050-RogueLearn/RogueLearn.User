using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("match_results")]
public class MatchResult : BaseEntity
{
    [Column("match_id")]
    public string MatchId { get; set; } = string.Empty;

    [Column("start_utc")]
    public DateTime StartUtc { get; set; }

    [Column("end_utc")]
    public DateTime EndUtc { get; set; }

    [Column("result")]
    public string Result { get; set; } = string.Empty; // "win" or "lose"

    [Column("scene")]
    public string Scene { get; set; } = string.Empty;

    [Column("total_players")]
    public int TotalPlayers { get; set; }

    // Store full match data as raw JSON string (avoids JsonElement serialization issues)
    [Column("match_data")]
    public string? MatchDataJson { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
