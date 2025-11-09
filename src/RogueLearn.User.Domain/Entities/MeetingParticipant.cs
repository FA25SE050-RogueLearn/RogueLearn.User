using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace RogueLearn.User.Domain.Entities;

[Table("meeting_participants")]
public class MeetingParticipant : BaseModel
{
    [PrimaryKey("participant_id")]
    public Guid ParticipantId { get; set; }

    [Column("meeting_id")]
    public Guid MeetingId { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("display_name")]
    public string? DisplayName { get; set; }

    [Column("join_time")]
    public DateTimeOffset? JoinTime { get; set; }

    [Column("leave_time")]
    public DateTimeOffset? LeaveTime { get; set; }

    [Column("role_in_meeting")]
    public string RoleInMeeting { get; set; } = "participant";
}