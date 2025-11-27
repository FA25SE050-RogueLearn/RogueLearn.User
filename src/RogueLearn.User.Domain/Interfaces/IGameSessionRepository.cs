using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IGameSessionRepository : IGenericRepository<GameSession>
{
    Task<GameSession?> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<GameSession?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default);
    Task<List<GameSession>> GetRecentSessionsAsync(int limit = 10, CancellationToken cancellationToken = default);
}
