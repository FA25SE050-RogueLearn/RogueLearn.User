using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IAchievementRepository : IGenericRepository<Achievement>
{
    Task<IEnumerable<Achievement>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}