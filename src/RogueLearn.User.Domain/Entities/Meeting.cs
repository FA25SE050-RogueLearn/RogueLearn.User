using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace RogueLearn.User.Domain.Entities;

[Table("meetings")]
public class Meeting : BaseModel
{
    [PrimaryKey("meeting_id")]
    public Guid MeetingId { get; set; }

    [Column("party_id")]
    public Guid? PartyId { get; set; }

    [Column("guild_id")]
    public Guid? GuildId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("scheduled_start_time")]
    public DateTimeOffset ScheduledStartTime { get; set; }

    [Column("scheduled_end_time")]
    public DateTimeOffset ScheduledEndTime { get; set; }

    [Column("actual_start_time")]
    public DateTimeOffset? ActualStartTime { get; set; }

    [Column("actual_end_time")]
    public DateTimeOffset? ActualEndTime { get; set; }

    [Column("organizer_id")]
    public Guid OrganizerId { get; set; }

    [Column("meeting_link")]
    public string? MeetingLink { get; set; }

    [Column("meeting_code")]
    public string? MeetingCode { get; set; }

    [Column("space_name")]
    public string? SpaceName { get; set; }

    [Column("meeting_status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public MeetingStatus? Status { get; set; } = MeetingStatus.Scheduled;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}