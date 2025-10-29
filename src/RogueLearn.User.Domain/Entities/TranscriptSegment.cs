using System.Text.Json.Serialization;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace RogueLearn.User.Domain.Entities;

[Table("transcript_segments")]
public class TranscriptSegment : BaseModel
{
    [PrimaryKey("segment_id")]
    public Guid SegmentId { get; set; }

    [Column("meeting_id")]
    public Guid MeetingId { get; set; }

    [Column("speaker_id")]
    public Guid SpeakerId { get; set; }

    [Column("start_time")]
    public DateTimeOffset StartTime { get; set; }

    [Column("end_time")]
    public DateTimeOffset EndTime { get; set; }

    [Column("transcript_text")]
    public string TranscriptText { get; set; } = string.Empty;

    [Column("chunk_number")]
    public int ChunkNumber { get; set; }

    [JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    [Column("status")]
    public TranscriptSegmentStatus Status { get; set; } = TranscriptSegmentStatus.Processed;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}