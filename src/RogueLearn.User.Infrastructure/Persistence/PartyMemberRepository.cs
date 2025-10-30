using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;

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
            .Filter("party_id", Operator.Equals, partyId.ToString())
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<PartyMember?> GetMemberAsync(Guid partyId, Guid authUserId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<PartyMember>()
            .Filter("party_id", Operator.Equals, partyId.ToString())
            .Filter("auth_user_id", Operator.Equals, authUserId.ToString())
            .Single(cancellationToken);

        return response;
    }

    public async Task<bool> IsLeaderAsync(Guid partyId, Guid authUserId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<PartyMember>()
            .Filter("party_id", Operator.Equals, partyId.ToString())
            .Filter("auth_user_id", Operator.Equals, authUserId.ToString())
            .Filter("role", Operator.Equals, PartyRole.Leader.ToString())
            .Get(cancellationToken);

        return response.Models.Any();
    }

    public async Task<int> CountActiveMembersAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<PartyMember>()
            .Filter("party_id", Operator.Equals, partyId.ToString())
            .Filter("status", Operator.Equals, MemberStatus.Active.ToString())
            .Get(cancellationToken);

        return response.Models.Count;
    }
}