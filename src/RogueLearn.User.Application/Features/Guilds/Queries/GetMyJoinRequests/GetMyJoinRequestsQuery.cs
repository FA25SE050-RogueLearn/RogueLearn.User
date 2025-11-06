using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetMyJoinRequests;

public record GetMyJoinRequestsQuery(Guid AuthUserId, bool PendingOnly = true) : IRequest<IReadOnlyList<GuildJoinRequestDto>>;