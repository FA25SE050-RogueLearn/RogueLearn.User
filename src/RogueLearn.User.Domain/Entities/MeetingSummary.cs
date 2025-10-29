using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace RogueLearn.User.Domain.Entities;

[Table("meeting_summaries")]
public class MeetingSummary : BaseModel
{
    [PrimaryKey("meeting_summary_id")]
    public Guid MeetingSummaryId { get; set; }

    [Column("meeting_id")]
    public Guid MeetingId { get; set; }

    [Column("summary_text")]
    public string SummaryText { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}