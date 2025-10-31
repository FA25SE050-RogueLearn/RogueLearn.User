using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class PartyStashItemRepository : GenericRepository<PartyStashItem>, IPartyStashItemRepository
{
    public PartyStashItemRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IEnumerable<PartyStashItem>> GetResourcesByPartyAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<PartyStashItem>()
            .Where(r => r.PartyId == partyId)
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<IEnumerable<PartyStashItem>> GetResourcesByPartyAndSubjectAsync(Guid partyId, string subject, CancellationToken cancellationToken = default)
    {
        // Subject is stored within Content dictionary; filter client-side for now
        var response = await _supabaseClient
            .From<PartyStashItem>()
            .Where(r => r.PartyId == partyId)
            .Get(cancellationToken);

        return response.Models.Where(m => m.Content.TryGetValue("subject", out var s) && string.Equals(s?.ToString(), subject, StringComparison.OrdinalIgnoreCase));
    }
}