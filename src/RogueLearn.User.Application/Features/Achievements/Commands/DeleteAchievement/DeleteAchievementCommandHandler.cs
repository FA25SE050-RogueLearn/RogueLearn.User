using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Achievements.Commands.DeleteAchievement;

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

    public async Task Handle(DeleteAchievementCommand request, CancellationToken cancellationToken)
    {
        var achievement = await _achievementRepository.GetByIdAsync(request.Id, cancellationToken);
        if (achievement is null)
        {
            throw new NotFoundException("Achievement", request.Id);
        }

        await _achievementRepository.DeleteAsync(request.Id, cancellationToken);
        _logger.LogInformation("Achievement '{Name}' with ID {Id} deleted", achievement.Name, achievement.Id);
    }
}