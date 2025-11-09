namespace RogueLearn.User.Application.Features.Meetings.DTOs;

public class MeetingParticipantDto
{
    public Guid ParticipantId { get; set; }
    public Guid MeetingId { get; set; }
    public Guid? UserId { get; set; }
    public string? DisplayName { get; set; }
    public string RoleInMeeting { get; set; } = "participant";
    public DateTimeOffset? JoinTime { get; set; }
    public DateTimeOffset? LeaveTime { get; set; }
}