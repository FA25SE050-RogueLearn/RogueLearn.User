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
            .Where(pi => pi.PartyId == partyId)
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<IEnumerable<PartyInvitation>> GetPendingInvitationsByPartyAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<PartyInvitation>()
            .Where(pi => pi.PartyId == partyId && pi.Status == InvitationStatus.Pending)
            .Get(cancellationToken);

        return response.Models;
    }
}