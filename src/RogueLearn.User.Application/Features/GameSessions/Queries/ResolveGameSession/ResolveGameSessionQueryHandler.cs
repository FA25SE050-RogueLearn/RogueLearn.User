using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.GameSessions.Queries.ResolveGameSession;

public sealed class ResolveGameSessionQueryHandler : IRequestHandler<ResolveGameSessionQuery, GameSession?>
{
    private readonly IGameSessionRepository _gameSessionRepository;

    public ResolveGameSessionQueryHandler(IGameSessionRepository gameSessionRepository)
    {
        _gameSessionRepository = gameSessionRepository;
    }

    public async Task<GameSession?> Handle(ResolveGameSessionQuery request, CancellationToken cancellationToken)
    {
        GameSession? session = null;
        if (!string.IsNullOrWhiteSpace(request.JoinCode))
        {
            session = await _gameSessionRepository.GetByJoinCodeAsync(request.JoinCode.Trim(), cancellationToken);
            if (session != null) return session;
        }
        if (!string.IsNullOrWhiteSpace(request.MatchId) && Guid.TryParse(request.MatchId, out var sessionGuid))
        {
            session = await _gameSessionRepository.GetBySessionIdAsync(sessionGuid, cancellationToken);
        }
        return session;
    }
}

