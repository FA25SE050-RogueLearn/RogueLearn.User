using MediatR;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Queries.GetMyJoinRequests;

public class GetMyJoinRequestsQueryHandler : IRequestHandler<GetMyJoinRequestsQuery, IReadOnlyList<GuildJoinRequestDto>>
{
    private readonly IGuildJoinRequestRepository _joinRequestRepository;
    private readonly IUserProfileRepository _userProfileRepository;

    public GetMyJoinRequestsQueryHandler(IGuildJoinRequestRepository joinRequestRepository, IUserProfileRepository userProfileRepository)
    {
        _joinRequestRepository = joinRequestRepository;
        _userProfileRepository = userProfileRepository;
    }

    public async Task<IReadOnlyList<GuildJoinRequestDto>> Handle(GetMyJoinRequestsQuery request, CancellationToken cancellationToken)
    {
        var list = await _joinRequestRepository.GetRequestsByRequesterAsync(request.AuthUserId, cancellationToken);
        if (request.PendingOnly)
        {
            list = list.Where(r => r.Status == GuildJoinRequestStatus.Pending).ToList();
        }

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