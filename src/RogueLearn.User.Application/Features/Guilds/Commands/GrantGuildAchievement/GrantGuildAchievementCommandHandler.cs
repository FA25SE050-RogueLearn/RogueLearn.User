using MediatR;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Features.Guilds.Commands.UpdateGuildMeritPoints;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.Guilds.Commands.GrantGuildAchievement;

public class GrantGuildAchievementCommandHandler : IRequestHandler<GrantGuildAchievementCommand, Unit>
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<GrantGuildAchievementCommandHandler> _logger;

    public GrantGuildAchievementCommandHandler(
        IAchievementRepository achievementRepository,
        IMediator mediator,
        ILogger<GrantGuildAchievementCommandHandler> logger)
    {
        _achievementRepository = achievementRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Unit> Handle(GrantGuildAchievementCommand request, CancellationToken cancellationToken)
    {
        var achievement = await _achievementRepository.FirstOrDefaultAsync(a => a.Key == request.AchievementKey, cancellationToken)
            ?? throw new Application.Exceptions.NotFoundException("Achievement", request.AchievementKey);

        var points = achievement.MeritPointsReward ?? 0;
        if (points != 0)
        {
            await _mediator.Send(new UpdateGuildMeritPointsCommand(request.GuildId, points), cancellationToken);
            _logger.LogInformation("Granted guild achievement {Key} to {GuildId} with {Points} merit points", request.AchievementKey, request.GuildId, points);
        }

        return Unit.Value;
    }
}