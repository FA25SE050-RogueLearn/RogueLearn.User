using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IPartyStashItemRepository : IGenericRepository<PartyStashItem>
{
    Task<IEnumerable<PartyStashItem>> GetResourcesByPartyAsync(Guid partyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PartyStashItem>> GetResourcesByPartyAndSubjectAsync(Guid partyId, string subject, CancellationToken cancellationToken = default);
}