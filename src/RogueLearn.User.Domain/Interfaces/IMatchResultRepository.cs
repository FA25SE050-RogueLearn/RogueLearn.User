using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IMatchResultRepository : IGenericRepository<MatchResult>
{
    Task<MatchResult?> GetByMatchIdAsync(Guid matchId, CancellationToken cancellationToken = default);
    Task<List<MatchResult>> GetRecentMatchesAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<List<MatchResult>> GetMatchesByUserAsync(Guid userId, int limit = 10, CancellationToken cancellationToken = default);
}
