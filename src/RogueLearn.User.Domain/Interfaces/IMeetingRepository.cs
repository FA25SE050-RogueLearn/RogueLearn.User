using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IMeetingRepository
{
    Task<bool> ExistsAsync(Guid meetingId, CancellationToken cancellationToken = default);
    Task<Meeting?> GetByIdAsync(Guid meetingId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Meeting>> GetByPartyAsync(Guid partyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Meeting>> GetByGuildAsync(Guid guildId, CancellationToken cancellationToken = default);
    Task<Meeting> AddAsync(Meeting entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Meeting entity, CancellationToken cancellationToken = default);
}