namespace RogueLearn.User.Application.Features.Meetings.DTOs;

public class MeetingDto
{
    public Guid MeetingId { get; set; }
    public Guid PartyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset ScheduledStartTime { get; set; }
    public DateTimeOffset ScheduledEndTime { get; set; }
    public DateTimeOffset? ActualStartTime { get; set; }
    public DateTimeOffset? ActualEndTime { get; set; }
    public Guid OrganizerId { get; set; }
}