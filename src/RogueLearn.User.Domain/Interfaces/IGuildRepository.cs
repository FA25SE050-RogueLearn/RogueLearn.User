using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IGuildRepository : IGenericRepository<Guild>
{
    Task<IEnumerable<Guild>> GetGuildsByCreatorAsync(Guid authUserId, CancellationToken cancellationToken = default);
}