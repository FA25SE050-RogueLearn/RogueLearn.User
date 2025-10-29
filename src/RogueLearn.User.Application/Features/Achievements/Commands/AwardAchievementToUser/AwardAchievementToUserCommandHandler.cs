using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.Achievements.Commands.AwardAchievementToUser;

/// <summary>
/// Handles awarding an existing Achievement to a user.
/// - Validates input via FluentValidation.
/// - Checks user and achievement existence.
/// - Prevents duplicate awarding.
/// - Emits structured logs for observability.
/// </summary>
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

    /// <summary>
    /// Awards the achievement to the specified user.
    /// </summary>
    public async Task Handle(AwardAchievementToUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling AwardAchievementToUserCommand for UserId={UserId}, AchievementId={AchievementId}", request.UserId, request.AchievementId);

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

        // Prevent duplicate awarding
        var alreadyEarned = await _userAchievementRepository.AnyAsync(
            ua => ua.AuthUserId == user.AuthUserId && ua.AchievementId == request.AchievementId,
            cancellationToken);
        if (alreadyEarned)
        {
            _logger.LogWarning("Duplicate award prevented: UserId={UserId}, AchievementId={AchievementId}", request.UserId, request.AchievementId);
            throw new BadRequestException($"User already earned achievement '{achievement.Name}'.");
        }

        // Parse optional Context JSON string into a structured object
        Dictionary<string, object>? contextDict = null;
        if (!string.IsNullOrWhiteSpace(request.Context))
        {
            try
            {
                using var doc = JsonDocument.Parse(request.Context);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    contextDict = JsonSerializer.Deserialize<Dictionary<string, object>>(request.Context);
                }
                else
                {
                    throw new RogueLearn.User.Application.Exceptions.ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Context", "Context must be a valid JSON object.") });
                }
            }
            catch (JsonException)
            {
                throw new RogueLearn.User.Application.Exceptions.ValidationException(new[] { new FluentValidation.Results.ValidationFailure("Context", "Context must be a valid JSON object.") });
            }
        }

        var userAchievement = new UserAchievement
        {
            Id = Guid.NewGuid(),
            AuthUserId = user.AuthUserId,
            AchievementId = request.AchievementId,
            EarnedAt = DateTimeOffset.UtcNow,
            Context = contextDict
        };

        await _userAchievementRepository.AddAsync(userAchievement, cancellationToken);

        _logger.LogInformation(
            "Achievement '{AchievementName}' awarded to user '{Username}' (AuthUserId: {AuthUserId})",
            achievement.Name, user.Username, user.AuthUserId);
    }
}