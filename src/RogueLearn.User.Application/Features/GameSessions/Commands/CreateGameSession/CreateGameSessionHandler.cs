using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.GameSessions.Commands.CreateGameSession;

public sealed class CreateGameSessionHandler : IRequestHandler<CreateGameSessionCommand, GameSession>
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly ILogger<CreateGameSessionHandler> _logger;

    public CreateGameSessionHandler(IGameSessionRepository gameSessionRepository, ILogger<CreateGameSessionHandler> logger)
    {
        _gameSessionRepository = gameSessionRepository;
        _logger = logger;
    }

    public async Task<GameSession> Handle(CreateGameSessionCommand request, CancellationToken cancellationToken)
    {
        var session = new GameSession
        {
            SessionId = request.SessionId,
            UserId = request.UserId,
            RelayJoinCode = request.RelayJoinCode?.Trim(),
            PackId = request.PackId,
            Subject = request.Subject,
            Topic = request.Topic,
            Difficulty = request.Difficulty,
            QuestionPackJson = request.QuestionPackJson,
            Status = "created"
        };

        var saved = await _gameSessionRepository.AddAsync(session, cancellationToken);
        _logger.LogInformation("[GameSession] Created session {SessionId} with pack (subject: {Subject}, topic: {Topic}, join code: {JoinCode})",
            request.SessionId, request.Subject, request.Topic, request.RelayJoinCode ?? "none");
        return saved;
    }
}
