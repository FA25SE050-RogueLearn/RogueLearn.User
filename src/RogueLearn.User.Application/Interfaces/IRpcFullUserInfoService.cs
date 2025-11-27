using RogueLearn.User.Application.Features.UserFullInfo.Queries.GetFullUserInfo;

namespace RogueLearn.User.Application.Interfaces;

public interface IRpcFullUserInfoService
{
    Task<FullUserInfoResponse?> GetAsync(Guid authUserId, int pageSize, int pageNumber, CancellationToken cancellationToken = default);
}