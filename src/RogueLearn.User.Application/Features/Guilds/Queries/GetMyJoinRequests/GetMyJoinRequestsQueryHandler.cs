using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetMyJoinRequests;

public class GetMyJoinRequestsQueryHandler : IRequestHandler<GetMyJoinRequestsQuery, IReadOnlyList<GuildJoinRequestDto>>
{
    private readonly IGuildJoinRequestRepository _joinRequestRepository;

    public GetMyJoinRequestsQueryHandler(IGuildJoinRequestRepository joinRequestRepository)
    {
        _joinRequestRepository = joinRequestRepository;
    }

    public async Task<IReadOnlyList<GuildJoinRequestDto>> Handle(GetMyJoinRequestsQuery request, CancellationToken cancellationToken)
    {
        var list = await _joinRequestRepository.GetRequestsByRequesterAsync(request.AuthUserId, cancellationToken);
        if (request.PendingOnly)
        {
            list = list.Where(r => r.Status == GuildJoinRequestStatus.Pending).ToList();
        }

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