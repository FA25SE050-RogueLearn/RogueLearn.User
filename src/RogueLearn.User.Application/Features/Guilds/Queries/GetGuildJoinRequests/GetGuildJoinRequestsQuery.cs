using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetGuildJoinRequests;

public record GetGuildJoinRequestsQuery(Guid GuildId, bool PendingOnly = true) : IRequest<IReadOnlyList<GuildJoinRequestDto>>;