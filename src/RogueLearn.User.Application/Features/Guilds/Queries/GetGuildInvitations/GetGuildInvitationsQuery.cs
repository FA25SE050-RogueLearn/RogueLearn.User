using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetGuildInvitations;

public record GetGuildInvitationsQuery(Guid GuildId) : IRequest<IEnumerable<GuildInvitationDto>>;