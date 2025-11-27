using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class PartyRepository : GenericRepository<Party>, IPartyRepository
{
    public PartyRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IEnumerable<Party>> GetPartiesByCreatorAsync(Guid authUserId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<Party>()
            .Where(p => p.CreatedBy == authUserId)
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<IEnumerable<Party>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var list = ids.Select(id => id.ToString()).ToList();
        if (!list.Any()) return Enumerable.Empty<Party>();

        var response = await _supabaseClient
            .From<Party>()
            .Filter("id", Supabase.Postgrest.Constants.Operator.In, list)
            .Get(cancellationToken);

        return response.Models;
    }
}