using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace RogueLearn.User.Domain.Entities;

[Table("summary_chunks")]
public class SummaryChunk : BaseModel
{
    [PrimaryKey("summary_chunk_id")]
    public Guid SummaryChunkId { get; set; }

    [Column("meeting_id")]
    public Guid MeetingId { get; set; }

    [Column("chunk_number")]
    public int ChunkNumber { get; set; }

    [Column("summary_text")]
    public string SummaryText { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}