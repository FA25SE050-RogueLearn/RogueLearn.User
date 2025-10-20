using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Achievements.Queries.GetUserAchievementsByAuthId;

public class GetUserAchievementsByAuthIdQueryHandler : IRequestHandler<GetUserAchievementsByAuthIdQuery, GetUserAchievementsByAuthIdResponse>
{
    private readonly IUserAchievementRepository _userAchievementRepository;
    private readonly IAchievementRepository _achievementRepository;

    public GetUserAchievementsByAuthIdQueryHandler(
        IUserAchievementRepository userAchievementRepository,
        IAchievementRepository achievementRepository)
    {
        _userAchievementRepository = userAchievementRepository;
        _achievementRepository = achievementRepository;
    }

    public async Task<GetUserAchievementsByAuthIdResponse> Handle(GetUserAchievementsByAuthIdQuery request, CancellationToken cancellationToken)
    {
        var userAchievements = await _userAchievementRepository.FindAsync(ua => ua.AuthUserId == request.AuthUserId, cancellationToken);
        var results = new List<UserAchievementDto>();

        foreach (var ua in userAchievements)
        {
            var achievement = await _achievementRepository.GetByIdAsync(ua.AchievementId, cancellationToken);
            if (achievement is null)
            {
                // Skip if achievement no longer exists (should be rare)
                continue;
            }

            results.Add(new UserAchievementDto
            {
                AchievementId = achievement.Id,
                Key = achievement.Key,
                Name = achievement.Name,
                Description = achievement.Description,
                IconUrl = achievement.IconUrl,
                SourceService = achievement.SourceService,
                EarnedAt = ua.EarnedAt,
                Context = ua.Context
            });
        }

        return new GetUserAchievementsByAuthIdResponse
        {
            Achievements = results
        };
    }
}