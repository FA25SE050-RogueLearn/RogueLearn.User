using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IGuildInvitationRepository : IGenericRepository<GuildInvitation>
{
    Task<IEnumerable<GuildInvitation>> GetInvitationsByGuildAsync(Guid guildId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GuildInvitation>> GetPendingInvitationsByGuildAsync(Guid guildId, CancellationToken cancellationToken = default);
}