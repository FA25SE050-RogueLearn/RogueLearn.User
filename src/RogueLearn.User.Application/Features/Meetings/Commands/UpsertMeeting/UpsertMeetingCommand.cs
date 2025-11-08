using MediatR;
using RogueLearn.User.Application.Features.Meetings.DTOs;

namespace RogueLearn.User.Application.Features.Meetings.Commands.UpsertMeeting;

public record UpsertMeetingCommand(MeetingDto MeetingDto) : IRequest<MeetingDto>;