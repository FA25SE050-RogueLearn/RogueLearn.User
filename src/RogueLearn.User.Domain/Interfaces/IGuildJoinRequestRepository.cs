using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IGuildJoinRequestRepository : IGenericRepository<GuildJoinRequest>
{
    Task<IReadOnlyList<GuildJoinRequest>> GetRequestsByGuildAsync(Guid guildId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GuildJoinRequest>> GetPendingRequestsByGuildAsync(Guid guildId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GuildJoinRequest>> GetRequestsByRequesterAsync(Guid requesterAuthUserId, CancellationToken cancellationToken = default);
}