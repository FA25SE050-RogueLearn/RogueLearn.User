using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IGuildPostRepository : IGenericRepository<GuildPost>
{
    Task<IEnumerable<GuildPost>> GetByGuildAsync(
        Guid guildId,
        string? tag = null,
        Guid? authorId = null,
        bool? pinned = null,
        string? search = null,
        int page = 1,
        int size = 20,
        CancellationToken cancellationToken = default);

    Task<GuildPost?> GetByIdAsync(Guid guildId, Guid postId, CancellationToken cancellationToken = default);

    Task<IEnumerable<GuildPost>> GetPinnedByGuildAsync(Guid guildId, CancellationToken cancellationToken = default);
}