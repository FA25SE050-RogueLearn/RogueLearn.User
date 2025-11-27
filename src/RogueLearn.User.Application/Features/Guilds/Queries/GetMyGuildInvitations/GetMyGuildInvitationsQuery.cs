using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetMyGuildInvitations;

public record GetMyGuildInvitationsQuery(Guid AuthUserId, bool PendingOnly = true) : IRequest<IReadOnlyList<GuildInvitationDto>>;