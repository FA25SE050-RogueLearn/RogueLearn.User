using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Achievements.Commands.DeleteAchievement;

/// <summary>
/// Handles deletion of an Achievement by Id.
/// - Throws standardized NotFoundException when missing.
/// - Emits structured logs for traceability.
/// </summary>
public class DeleteAchievementCommandHandler : IRequestHandler<DeleteAchievementCommand>
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly ILogger<DeleteAchievementCommandHandler> _logger;

    public DeleteAchievementCommandHandler(
        IAchievementRepository achievementRepository,
        ILogger<DeleteAchievementCommandHandler> logger)
    {
        _achievementRepository = achievementRepository;
        _logger = logger;
    }

    /// <summary>
    /// Deletes an achievement by Id.
    /// </summary>
    public async Task Handle(DeleteAchievementCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling DeleteAchievementCommand for Id={AchievementId}", request.Id);

        var achievement = await _achievementRepository.GetByIdAsync(request.Id, cancellationToken);
        if (achievement is null)
        {
            _logger.LogWarning("Achievement not found: Id={AchievementId}", request.Id);
            throw new NotFoundException("Achievement", request.Id);
        }

        await _achievementRepository.DeleteAsync(request.Id, cancellationToken);
        _logger.LogInformation("Achievement '{Name}' with ID {Id} deleted", achievement.Name, achievement.Id);
    }
}