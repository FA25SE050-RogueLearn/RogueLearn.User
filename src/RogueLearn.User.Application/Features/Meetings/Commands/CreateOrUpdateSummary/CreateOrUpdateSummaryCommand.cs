using MediatR;

namespace RogueLearn.User.Application.Features.Meetings.Commands.CreateOrUpdateSummary;

public record CreateOrUpdateSummaryCommand(Guid MeetingId, string Content) : IRequest<Unit>;