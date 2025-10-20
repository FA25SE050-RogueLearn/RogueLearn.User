using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Achievements.Commands.RevokeAchievementFromUser;

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

    public async Task Handle(RevokeAchievementFromUserCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        // Verify user exists
        var user = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("User", request.UserId);
        }

        // Verify achievement exists
        var achievement = await _achievementRepository.GetByIdAsync(request.AchievementId, cancellationToken);
        if (achievement is null)
        {
            throw new NotFoundException("Achievement", request.AchievementId);
        }

        // Find the specific user achievement
        var userAchievements = await _userAchievementRepository.FindAsync(
            ua => ua.AuthUserId == user.AuthUserId && ua.AchievementId == request.AchievementId,
            cancellationToken);
        var uaToRemove = userAchievements.FirstOrDefault();

        if (uaToRemove is null)
        {
            throw new BadRequestException($"User has not earned achievement '{achievement.Name}'.");
        }

        await _userAchievementRepository.DeleteAsync(uaToRemove.Id, cancellationToken);

        _logger.LogInformation(
            "Achievement '{AchievementName}' revoked from user '{Username}' (AuthUserId: {AuthUserId})",
            achievement.Name, user.Username, user.AuthUserId);
    }
}