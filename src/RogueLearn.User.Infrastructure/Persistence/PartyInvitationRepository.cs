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
        var response = await _supabaseClient
            .From<PartyInvitation>()
            .Filter("party_id", Supabase.Postgrest.Constants.Operator.Equals, partyId.ToString())
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<IEnumerable<PartyInvitation>> GetPendingInvitationsByPartyAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
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

    public async Task<PartyInvitation?> GetByPartyAndInviteeAsync(Guid partyId, Guid inviteeAuthUserId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<PartyInvitation>()
            .Filter("party_id", Supabase.Postgrest.Constants.Operator.Equals, partyId.ToString())
            .Filter("invitee_id", Supabase.Postgrest.Constants.Operator.Equals, inviteeAuthUserId.ToString())
            .Get(cancellationToken);

        return response.Models.FirstOrDefault();
    }
}