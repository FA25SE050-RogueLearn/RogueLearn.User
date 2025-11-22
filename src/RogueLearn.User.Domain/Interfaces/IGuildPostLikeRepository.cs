using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IGuildPostLikeRepository : IGenericRepository<GuildPostLike>
{
    Task<GuildPostLike?> GetByPostAndUserAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default);
    Task<int> CountByPostAsync(Guid postId, CancellationToken cancellationToken = default);
}