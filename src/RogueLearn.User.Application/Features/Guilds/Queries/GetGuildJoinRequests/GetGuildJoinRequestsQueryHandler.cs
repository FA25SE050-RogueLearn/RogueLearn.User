using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetGuildJoinRequests;

public class GetGuildJoinRequestsQueryHandler : IRequestHandler<GetGuildJoinRequestsQuery, IReadOnlyList<GuildJoinRequestDto>>
{
    private readonly IGuildJoinRequestRepository _joinRequestRepository;
    private readonly IUserProfileRepository _userProfileRepository;

    public GetGuildJoinRequestsQueryHandler(IGuildJoinRequestRepository joinRequestRepository, IUserProfileRepository userProfileRepository)
    {
        _joinRequestRepository = joinRequestRepository;
        _userProfileRepository = userProfileRepository;
    }

    public async Task<IReadOnlyList<GuildJoinRequestDto>> Handle(GetGuildJoinRequestsQuery request, CancellationToken cancellationToken)
    {
        var list = request.PendingOnly
            ? await _joinRequestRepository.GetPendingRequestsByGuildAsync(request.GuildId, cancellationToken)
            : await _joinRequestRepository.GetRequestsByGuildAsync(request.GuildId, cancellationToken);

        var results = new List<GuildJoinRequestDto>();
        foreach (var r in list)
        {
            var profile = await _userProfileRepository.GetByAuthIdAsync(r.RequesterId, cancellationToken);
            var name = (string.IsNullOrWhiteSpace(profile?.FirstName) && string.IsNullOrWhiteSpace(profile?.LastName))
                ? (profile?.Username ?? string.Empty)
                : $"{profile?.FirstName} {profile?.LastName}".Trim();

            results.Add(new GuildJoinRequestDto
            {
                Id = r.Id,
                GuildId = r.GuildId,
                RequesterId = r.RequesterId,
                Status = r.Status,
                Message = r.Message,
                CreatedAt = r.CreatedAt,
                RespondedAt = r.RespondedAt,
                ExpiresAt = r.ExpiresAt,
                RequesterName = name
            });
        }

        return results;
    }
}