using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IPartyRepository : IGenericRepository<Party>
{
    Task<IEnumerable<Party>> GetPartiesByCreatorAsync(Guid authUserId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Party>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}