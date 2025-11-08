using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface ITagRepository : IGenericRepository<Tag>
{
    /// <summary>
    /// Fetches tags by a list of IDs using an efficient Supabase 'in' filter.
    /// </summary>
    /// <param name="ids">Collection of tag IDs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Enumerable of tags matching the provided IDs.</returns>
    Task<IEnumerable<Tag>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}