using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

public class GuildPostLikeRepository : GenericRepository<GuildPostLike>, IGuildPostLikeRepository
{
    public GuildPostLikeRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<GuildPostLike?> GetByPostAndUserAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GuildPostLike>()
            .Filter("post_id", Operator.Equals, postId.ToString())
            .Filter("user_id", Operator.Equals, userId.ToString())
            .Single(cancellationToken);

        return response;
    }

    public async Task<int> CountByPostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var count = await _supabaseClient
            .From<GuildPostLike>()
            .Filter("post_id", Operator.Equals, postId.ToString())
            .Count(CountType.Exact);

        return count;
    }
}