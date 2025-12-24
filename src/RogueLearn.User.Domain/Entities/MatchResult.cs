using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("match_results")]
public class MatchResult : BaseEntity
{
    [Column("match_id")]
    public Guid MatchId { get; set; }

    [Column("start_utc")]
    public DateTime StartUtc { get; set; }

    [Column("end_utc")]
    public DateTime EndUtc { get; set; }

    [Column("result")]
    public string Result { get; set; } = string.Empty;

    [Column("scene")]
    public string Scene { get; set; } = string.Empty;

    [Column("total_players")]
    public int TotalPlayers { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("match_data")]
    public string? MatchDataJson { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
