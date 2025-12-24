using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IMatchPlayerSummaryRepository : IGenericRepository<MatchPlayerSummary>
{
    Task<List<MatchPlayerSummary>> GetByMatchResultIdAsync(Guid matchResultId, CancellationToken cancellationToken = default);
    Task DeleteByMatchResultIdAsync(Guid matchResultId, CancellationToken cancellationToken = default);
    Task<MatchPlayerSummary?> GetLatestByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetRecentMatchResultIdsByUserAsync(Guid userId, int limit = 10, CancellationToken cancellationToken = default);
}
