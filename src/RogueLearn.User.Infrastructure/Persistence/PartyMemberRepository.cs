using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class PartyMemberRepository : GenericRepository<PartyMember>, IPartyMemberRepository
{
    public PartyMemberRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IEnumerable<PartyMember>> GetMembersByPartyAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<PartyMember>()
            .Where(pm => pm.PartyId == partyId)
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<PartyMember?> GetMemberAsync(Guid partyId, Guid authUserId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<PartyMember>()
            .Where(pm => pm.PartyId == partyId && pm.AuthUserId == authUserId)
            .Single(cancellationToken);

        return response;
    }

    public async Task<bool> IsLeaderAsync(Guid partyId, Guid authUserId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<PartyMember>()
            .Where(pm => pm.PartyId == partyId && pm.AuthUserId == authUserId && pm.Role == PartyRole.Leader)
            .Get(cancellationToken);

        return response.Models.Any();
    }

    public async Task<int> CountActiveMembersAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<PartyMember>()
            .Where(pm => pm.PartyId == partyId && pm.Status == MemberStatus.Active)
            .Get(cancellationToken);

        return response.Models.Count;
    }
}