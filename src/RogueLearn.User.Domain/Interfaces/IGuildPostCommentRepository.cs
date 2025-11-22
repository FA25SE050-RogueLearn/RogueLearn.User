using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IGuildPostCommentRepository : IGenericRepository<GuildPostComment>
{
    Task<IEnumerable<GuildPostComment>> GetByPostAsync(Guid postId, int page = 1, int size = 20, CancellationToken cancellationToken = default);
    Task<int> CountByPostAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<int> CountRepliesAsync(Guid parentCommentId, CancellationToken cancellationToken = default);
}