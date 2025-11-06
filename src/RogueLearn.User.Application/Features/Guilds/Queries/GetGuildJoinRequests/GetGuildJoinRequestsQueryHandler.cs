using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetGuildJoinRequests;

public class GetGuildJoinRequestsQueryHandler : IRequestHandler<GetGuildJoinRequestsQuery, IReadOnlyList<GuildJoinRequestDto>>
{
    private readonly IGuildJoinRequestRepository _joinRequestRepository;

    public GetGuildJoinRequestsQueryHandler(IGuildJoinRequestRepository joinRequestRepository)
    {
        _joinRequestRepository = joinRequestRepository;
    }

    public async Task<IReadOnlyList<GuildJoinRequestDto>> Handle(GetGuildJoinRequestsQuery request, CancellationToken cancellationToken)
    {
        var list = request.PendingOnly
            ? await _joinRequestRepository.GetPendingRequestsByGuildAsync(request.GuildId, cancellationToken)
            : await _joinRequestRepository.GetRequestsByGuildAsync(request.GuildId, cancellationToken);

        return list.Select(r => new GuildJoinRequestDto
        {
            Id = r.Id,
            GuildId = r.GuildId,
            RequesterId = r.RequesterId,
            Status = r.Status,
            Message = r.Message,
            CreatedAt = r.CreatedAt,
            RespondedAt = r.RespondedAt,
            ExpiresAt = r.ExpiresAt
        }).ToList();
    }
}