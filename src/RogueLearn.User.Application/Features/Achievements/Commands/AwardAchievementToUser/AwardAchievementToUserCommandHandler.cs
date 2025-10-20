using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Achievements.Commands.AwardAchievementToUser;

public class AwardAchievementToUserCommandHandler : IRequestHandler<AwardAchievementToUserCommand>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IAchievementRepository _achievementRepository;
    private readonly IUserAchievementRepository _userAchievementRepository;
    private readonly IValidator<AwardAchievementToUserCommand> _validator;
    private readonly ILogger<AwardAchievementToUserCommandHandler> _logger;

    public AwardAchievementToUserCommandHandler(
        IUserProfileRepository userProfileRepository,
        IAchievementRepository achievementRepository,
        IUserAchievementRepository userAchievementRepository,
        IValidator<AwardAchievementToUserCommand> validator,
        ILogger<AwardAchievementToUserCommandHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _achievementRepository = achievementRepository;
        _userAchievementRepository = userAchievementRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task Handle(AwardAchievementToUserCommand request, CancellationToken cancellationToken)
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

        // Prevent duplicate awarding
        var alreadyEarned = await _userAchievementRepository.AnyAsync(
            ua => ua.AuthUserId == user.AuthUserId && ua.AchievementId == request.AchievementId,
            cancellationToken);
        if (alreadyEarned)
        {
            throw new BadRequestException($"User already earned achievement '{achievement.Name}'.");
        }

        var userAchievement = new UserAchievement
        {
            Id = Guid.NewGuid(),
            AuthUserId = user.AuthUserId,
            AchievementId = request.AchievementId,
            EarnedAt = DateTimeOffset.UtcNow,
            Context = string.IsNullOrWhiteSpace(request.Context) ? null : request.Context.Trim()
        };

        await _userAchievementRepository.AddAsync(userAchievement, cancellationToken);

        _logger.LogInformation(
            "Achievement '{AchievementName}' awarded to user '{Username}' (AuthUserId: {AuthUserId})",
            achievement.Name, user.Username, user.AuthUserId);
    }
}