using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IGuildMemberRepository : IGenericRepository<GuildMember>
{
    Task<IEnumerable<GuildMember>> GetMembersByGuildAsync(Guid guildId, CancellationToken cancellationToken = default);
    Task<GuildMember?> GetMemberAsync(Guid guildId, Guid authUserId, CancellationToken cancellationToken = default);
    Task<bool> IsGuildMasterAsync(Guid guildId, Guid authUserId, CancellationToken cancellationToken = default);
    Task<int> CountActiveMembersAsync(Guid guildId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GuildMember>> GetMembershipsByUserAsync(Guid authUserId, CancellationToken cancellationToken = default);
}