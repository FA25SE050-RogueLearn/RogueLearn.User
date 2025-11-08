using MediatR;
using RogueLearn.User.Application.Features.Meetings.DTOs;

namespace RogueLearn.User.Application.Features.Meetings.Queries.GetPartyMeetings;

public record GetPartyMeetingsQuery(Guid PartyId) : IRequest<IReadOnlyList<MeetingDto>>;