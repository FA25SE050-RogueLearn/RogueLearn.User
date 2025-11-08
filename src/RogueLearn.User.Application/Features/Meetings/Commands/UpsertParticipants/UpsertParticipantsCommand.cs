using MediatR;
using RogueLearn.User.Application.Features.Meetings.DTOs;

namespace RogueLearn.User.Application.Features.Meetings.Commands.UpsertParticipants;

public record UpsertParticipantsCommand(Guid MeetingId, List<MeetingParticipantDto> Participants) : IRequest<IReadOnlyList<MeetingParticipantDto>>;