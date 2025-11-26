using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class GuildRepository : GenericRepository<Guild>, IGuildRepository
{
    public GuildRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IEnumerable<Guild>> GetGuildsByCreatorAsync(Guid authUserId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<Guild>()
            .Where(g => g.CreatedBy == authUserId)
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<IEnumerable<Guild>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var list = ids.Select(id => id.ToString()).ToList();
        if (!list.Any()) return Enumerable.Empty<Guild>();

        var response = await _supabaseClient
            .From<Guild>()
            .Filter("id", Supabase.Postgrest.Constants.Operator.In, list)
            .Get(cancellationToken);

        return response.Models;
    }
}