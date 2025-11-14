using MediatR;
using RogueLearn.User.Application.Features.Meetings.DTOs;

namespace RogueLearn.User.Application.Features.Meetings.Queries.GetGuildMeetings;

public record GetGuildMeetingsQuery(Guid GuildId) : IRequest<IReadOnlyList<MeetingDto>>;