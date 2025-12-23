using MediatR;

namespace RogueLearn.User.Application.Features.Meetings.Commands.DeleteMeeting;

public record DeleteMeetingCommand(Guid MeetingId, Guid RequestorId) : IRequest<Unit>;
