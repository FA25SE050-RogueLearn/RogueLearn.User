using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class PartyInvitationRepository : GenericRepository<PartyInvitation>, IPartyInvitationRepository
{
    public PartyInvitationRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IEnumerable<PartyInvitation>> GetInvitationsByPartyAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        // Use explicit filters to avoid enum serialization issues in PostgREST
        var response = await _supabaseClient
            .From<PartyInvitation>()
            .Filter("party_id", Supabase.Postgrest.Constants.Operator.Equals, partyId.ToString())
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<IEnumerable<PartyInvitation>> GetPendingInvitationsByPartyAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        // IMPORTANT: Do not compare enum values via lambda Where; it serializes to numeric (e.g., "0"),
        // which causes Postgres enum type to reject the value. Use string representation via Filter instead.
        var response = await _supabaseClient
            .From<PartyInvitation>()
            .Filter("party_id", Supabase.Postgrest.Constants.Operator.Equals, partyId.ToString())
            .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, InvitationStatus.Pending.ToString())
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<IEnumerable<PartyInvitation>> GetPendingInvitationsByInviteeAsync(Guid inviteeAuthUserId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<PartyInvitation>()
            .Filter("invitee_id", Supabase.Postgrest.Constants.Operator.Equals, inviteeAuthUserId.ToString())
            .Filter("status", Supabase.Postgrest.Constants.Operator.Equals, InvitationStatus.Pending.ToString())
            .Get(cancellationToken);

        return response.Models;
    }
}