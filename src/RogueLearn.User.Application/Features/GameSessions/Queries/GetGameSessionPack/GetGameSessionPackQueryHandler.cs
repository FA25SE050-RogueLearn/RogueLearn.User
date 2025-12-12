using MediatR;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RogueLearn.User.Application.Features.GameSessions.Queries.GetGameSessionPack;

public sealed class GetGameSessionPackQueryHandler : IRequestHandler<GetGameSessionPackQuery, string?>
{
    private readonly IGameSessionRepository _gameSessionRepository;

    public GetGameSessionPackQueryHandler(IGameSessionRepository gameSessionRepository)
    {
        _gameSessionRepository = gameSessionRepository;
    }

    public async Task<string?> Handle(GetGameSessionPackQuery request, CancellationToken cancellationToken)
    {
        var gameSession = await _gameSessionRepository.GetBySessionIdAsync(request.SessionId, cancellationToken);
        if (gameSession == null || string.IsNullOrWhiteSpace(gameSession.QuestionPackJson))
        {
            return null;
        }
        return gameSession.QuestionPackJson;
    }
}

