using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Achievements.Commands.GrantAchievements;

public class GrantAchievementsCommandHandler : IRequestHandler<GrantAchievementsCommand, GrantAchievementsResponse>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IAchievementRepository _achievementRepository;
    private readonly IUserAchievementRepository _userAchievementRepository;
    private readonly ILogger<GrantAchievementsCommandHandler> _logger;

    public GrantAchievementsCommandHandler(
        IUserProfileRepository userProfileRepository,
        IAchievementRepository achievementRepository,
        IUserAchievementRepository userAchievementRepository,
        ILogger<GrantAchievementsCommandHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _achievementRepository = achievementRepository;
        _userAchievementRepository = userAchievementRepository;
        _logger = logger;
    }

    public async Task<GrantAchievementsResponse> Handle(GrantAchievementsCommand request, CancellationToken cancellationToken)
    {
        var result = new GrantAchievementsResponse();

        foreach (var entry in request.Entries)
        {
            if (!Guid.TryParse(entry.UserId, out var userId))
            {
                result.Errors.Add($"invalid user_id: {entry.UserId}");
                continue;
            }

            var user = await _userProfileRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                result.Errors.Add($"user not found: {entry.UserId}");
                continue;
            }

            var achievement = await _achievementRepository.FirstOrDefaultAsync(a => a.Key == entry.AchievementKey, cancellationToken);
            if (achievement is null)
            {
                result.Errors.Add($"achievement not found: {entry.AchievementKey}");
                continue;
            }

            var alreadyEarned = await _userAchievementRepository.AnyAsync(
                ua => ua.AuthUserId == user.AuthUserId && ua.AchievementId == achievement.Id,
                cancellationToken);
            if (alreadyEarned)
            {
                result.Errors.Add($"already earned: user={user.AuthUserId}, key={entry.AchievementKey}");
                continue;
            }

            var userAchievement = new UserAchievement
            {
                Id = Guid.NewGuid(),
                AuthUserId = user.AuthUserId,
                AchievementId = achievement.Id,
                EarnedAt = DateTimeOffset.UtcNow,
                Context = null
            };

            await _userAchievementRepository.AddAsync(userAchievement, cancellationToken);

            result.GrantedCount++;
            _logger.LogInformation("Granted achievement key={Key} to AuthUserId={AuthUserId}", entry.AchievementKey, user.AuthUserId);
        }

        return result;
    }
}