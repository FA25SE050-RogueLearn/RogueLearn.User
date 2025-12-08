using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.GameSessions.Commands.CompleteGameSession;

public sealed class CompleteGameSessionHandler : IRequestHandler<CompleteGameSessionCommand, CompleteGameSessionResponse>
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IMatchResultRepository _matchResultRepository;
    private readonly ILogger<CompleteGameSessionHandler> _logger;

    public CompleteGameSessionHandler(
        IGameSessionRepository gameSessionRepository,
        IMatchResultRepository matchResultRepository,
        ILogger<CompleteGameSessionHandler> logger)
    {
        _gameSessionRepository = gameSessionRepository;
        _matchResultRepository = matchResultRepository;
        _logger = logger;
    }

    public async Task<CompleteGameSessionResponse> Handle(CompleteGameSessionCommand request, CancellationToken cancellationToken)
    {
        var gameSession = await _gameSessionRepository.GetBySessionIdAsync(request.SessionId, cancellationToken);
        var alreadyCompleted = gameSession?.Status == "completed";

        if (!alreadyCompleted && gameSession != null)
        {
            gameSession.Status = "completed";
            gameSession.CompletedAt = DateTimeOffset.UtcNow;

            var existingMatch = await _matchResultRepository.GetByMatchIdAsync(request.SessionId.ToString(), cancellationToken);
            if (existingMatch != null)
            {
                gameSession.MatchResultId = existingMatch.Id;
                if (existingMatch.UserId.HasValue && !gameSession.UserId.HasValue)
                {
                    gameSession.UserId = existingMatch.UserId;
                }
            }

            await _gameSessionRepository.UpdateAsync(gameSession, cancellationToken);
            _logger.LogInformation("[GameSession] Marked session {SessionId} as completed", request.SessionId);
        }

        return new CompleteGameSessionResponse
        {
            MatchId = request.SessionId.ToString(),
            AlreadyCompleted = alreadyCompleted
        };
    }
}
