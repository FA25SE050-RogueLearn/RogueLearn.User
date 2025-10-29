using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.Achievements.Queries.GetUserAchievementsByAuthId;

/// <summary>
/// Handles retrieval of a user's earned achievements by their auth user id.
/// Adds structured logging and ensures robust behavior when referenced achievements were removed.
/// </summary>
public class GetUserAchievementsByAuthIdQueryHandler : IRequestHandler<GetUserAchievementsByAuthIdQuery, GetUserAchievementsByAuthIdResponse>
{
    private readonly IUserAchievementRepository _userAchievementRepository;
    private readonly IAchievementRepository _achievementRepository;
    private readonly ILogger<GetUserAchievementsByAuthIdQueryHandler> _logger;

    public GetUserAchievementsByAuthIdQueryHandler(
        IUserAchievementRepository userAchievementRepository,
        IAchievementRepository achievementRepository,
        ILogger<GetUserAchievementsByAuthIdQueryHandler> logger)
    {
        _userAchievementRepository = userAchievementRepository;
        _achievementRepository = achievementRepository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves user achievements for the specified auth user id and maps them to DTOs.
    /// </summary>
    /// <param name="request">The query request containing the auth user id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A response containing the user's achievements.</returns>
    public async Task<GetUserAchievementsByAuthIdResponse> Handle(GetUserAchievementsByAuthIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {Handler} - retrieving user achievements for AuthUserId={AuthUserId}", nameof(GetUserAchievementsByAuthIdQueryHandler), request.AuthUserId);

        var userAchievements = await _userAchievementRepository.FindAsync(ua => ua.AuthUserId == request.AuthUserId, cancellationToken);
        var results = new List<UserAchievementDto>();

        foreach (var ua in userAchievements)
        {
            var achievement = await _achievementRepository.GetByIdAsync(ua.AchievementId, cancellationToken);
            if (achievement is null)
            {
                _logger.LogWarning("{Handler} - referenced achievement not found for AchievementId={AchievementId}; skipping entry", nameof(GetUserAchievementsByAuthIdQueryHandler), ua.AchievementId);
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
                // Serialize structured context to JSON string for DTO
                Context = ua.Context != null ? JsonSerializer.Serialize(ua.Context) : null
            });
        }

        _logger.LogInformation("{Handler} - returning {Count} user achievements for AuthUserId={AuthUserId}", nameof(GetUserAchievementsByAuthIdQueryHandler), results.Count, request.AuthUserId);

        return new GetUserAchievementsByAuthIdResponse
        {
            Achievements = results
        };
    }
}