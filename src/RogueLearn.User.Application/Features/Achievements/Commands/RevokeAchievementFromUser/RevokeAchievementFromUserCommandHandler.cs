using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Achievements.Commands.RevokeAchievementFromUser;

/// <summary>
/// Handles revoking a previously awarded Achievement from a user.
/// - Validates input via FluentValidation.
/// - Checks user and achievement existence.
/// - Ensures the user actually has the achievement before revocation.
/// - Emits structured logs for observability.
/// </summary>
public class RevokeAchievementFromUserCommandHandler : IRequestHandler<RevokeAchievementFromUserCommand>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IAchievementRepository _achievementRepository;
    private readonly IUserAchievementRepository _userAchievementRepository;
    private readonly IValidator<RevokeAchievementFromUserCommand> _validator;
    private readonly ILogger<RevokeAchievementFromUserCommandHandler> _logger;

    public RevokeAchievementFromUserCommandHandler(
        IUserProfileRepository userProfileRepository,
        IAchievementRepository achievementRepository,
        IUserAchievementRepository userAchievementRepository,
        IValidator<RevokeAchievementFromUserCommand> validator,
        ILogger<RevokeAchievementFromUserCommandHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _achievementRepository = achievementRepository;
        _userAchievementRepository = userAchievementRepository;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Revokes the achievement from the specified user.
    /// </summary>
    public async Task Handle(RevokeAchievementFromUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling RevokeAchievementFromUserCommand for UserId={UserId}, AchievementId={AchievementId}", request.UserId, request.AchievementId);

        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        // Verify user exists
        var user = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("User not found: UserId={UserId}", request.UserId);
            throw new NotFoundException("User", request.UserId);
        }

        // Verify achievement exists
        var achievement = await _achievementRepository.GetByIdAsync(request.AchievementId, cancellationToken);
        if (achievement is null)
        {
            _logger.LogWarning("Achievement not found: AchievementId={AchievementId}", request.AchievementId);
            throw new NotFoundException("Achievement", request.AchievementId);
        }

        // Find the specific user achievement
        var userAchievements = await _userAchievementRepository.FindAsync(
            ua => ua.AuthUserId == user.AuthUserId && ua.AchievementId == request.AchievementId,
            cancellationToken);
        var uaToRemove = userAchievements.FirstOrDefault();

        if (uaToRemove is null)
        {
            _logger.LogWarning("No achievement to revoke: UserId={UserId}, AchievementId={AchievementId}", request.UserId, request.AchievementId);
            throw new BadRequestException($"User has not earned achievement '{achievement.Name}'.");
        }

        await _userAchievementRepository.DeleteAsync(uaToRemove.Id, cancellationToken);

        _logger.LogInformation(
            "Achievement '{AchievementName}' revoked from user '{Username}' (AuthUserId: {AuthUserId})",
            achievement.Name, user.Username, user.AuthUserId);
    }
}