using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

public class GuildPostCommentRepository : GenericRepository<GuildPostComment>, IGuildPostCommentRepository
{
    public GuildPostCommentRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IEnumerable<GuildPostComment>> GetByPostAsync(Guid postId, int page = 1, int size = 20, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GuildPostComment>()
            .Filter("post_id", Operator.Equals, postId.ToString())
            .Range((page - 1) * size, (page - 1) * size + size - 1)
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<int> CountByPostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var count = await _supabaseClient
            .From<GuildPostComment>()
            .Filter("post_id", Operator.Equals, postId.ToString())
            .Count(CountType.Exact);

        return count;
    }

    public async Task<int> CountRepliesAsync(Guid parentCommentId, CancellationToken cancellationToken = default)
    {
        var count = await _supabaseClient
            .From<GuildPostComment>()
            .Filter("parent_comment_id", Operator.Equals, parentCommentId.ToString())
            .Count(CountType.Exact);

        return count;
    }
}