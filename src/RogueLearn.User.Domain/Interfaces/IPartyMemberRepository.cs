using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Domain.Interfaces;

public interface IPartyMemberRepository : IGenericRepository<PartyMember>
{
    Task<IEnumerable<PartyMember>> GetMembersByPartyAsync(Guid partyId, CancellationToken cancellationToken = default);
    Task<PartyMember?> GetMemberAsync(Guid partyId, Guid authUserId, CancellationToken cancellationToken = default);
    Task<bool> IsLeaderAsync(Guid partyId, Guid authUserId, CancellationToken cancellationToken = default);
    Task<int> CountActiveMembersAsync(Guid partyId, CancellationToken cancellationToken = default);
}