using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Domain.Interfaces;

public interface IPartyInvitationRepository : IGenericRepository<PartyInvitation>
{
    Task<IEnumerable<PartyInvitation>> GetInvitationsByPartyAsync(Guid partyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PartyInvitation>> GetPendingInvitationsByPartyAsync(Guid partyId, CancellationToken cancellationToken = default);
}