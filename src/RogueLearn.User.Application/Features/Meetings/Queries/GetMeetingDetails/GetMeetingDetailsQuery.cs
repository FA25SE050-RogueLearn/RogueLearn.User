using MediatR;
using RogueLearn.User.Application.Features.Meetings.DTOs;

namespace RogueLearn.User.Application.Features.Meetings.Queries.GetMeetingDetails;

public record GetMeetingDetailsQuery(Guid MeetingId) : IRequest<MeetingDetailsDto>;