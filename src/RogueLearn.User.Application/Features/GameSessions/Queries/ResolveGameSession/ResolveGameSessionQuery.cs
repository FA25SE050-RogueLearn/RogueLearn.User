using MediatR;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Features.GameSessions.Queries.ResolveGameSession;

public sealed record ResolveGameSessionQuery(string? JoinCode, string? MatchId) : IRequest<GameSession?>;

