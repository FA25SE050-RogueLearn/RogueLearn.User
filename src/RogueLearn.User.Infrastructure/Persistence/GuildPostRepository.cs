using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

public class GuildPostRepository : GenericRepository<GuildPost>, IGuildPostRepository
{
    public GuildPostRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IEnumerable<GuildPost>> GetByGuildAsync(
        Guid guildId,
        string? tag = null,
        Guid? authorId = null,
        bool? pinned = null,
        string? search = null,
        int page = 1,
        int size = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _supabaseClient
            .From<GuildPost>()
            .Filter("guild_id", Operator.Equals, guildId.ToString());

        if (authorId.HasValue)
        {
            query = query.Filter("author_id", Operator.Equals, authorId.Value.ToString());
        }

        if (pinned.HasValue)
        {
            query = query.Filter("is_pinned", Operator.Equals, pinned.Value ? "true" : "false");
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            query = query.Filter("tags", Operator.Contains, tag);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Filter("title", Operator.ILike, $"%{search}%");
        }

        var response = await query.Range((page - 1) * size, (page - 1) * size + size - 1).Get(cancellationToken);
        return response.Models;
    }

    public async Task<GuildPost?> GetByIdAsync(Guid guildId, Guid postId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GuildPost>()
            .Filter("guild_id", Operator.Equals, guildId.ToString())
            .Filter("id", Operator.Equals, postId.ToString())
            .Single(cancellationToken);
        return response;
    }

    public async Task<IEnumerable<GuildPost>> GetPinnedByGuildAsync(Guid guildId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<GuildPost>()
            .Filter("guild_id", Operator.Equals, guildId.ToString())
            .Filter("is_pinned", Operator.Equals, "true")
            .Get(cancellationToken);
        return response.Models;
    }
}