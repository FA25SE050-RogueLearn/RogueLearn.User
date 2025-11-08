namespace RogueLearn.User.Application.Features.Meetings.DTOs;

public class MeetingDetailsDto
{
    public MeetingDto Meeting { get; set; } = new();
    public List<MeetingParticipantDto> Participants { get; set; } = new();
    public string? SummaryText { get; set; }
}