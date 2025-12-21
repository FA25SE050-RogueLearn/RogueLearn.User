using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IRoleRepository : IGenericRepository<Role>
{
	Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
	Task<IEnumerable<Role>> GetByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);	
}