using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IMeetingParticipantRepository
{
    Task<bool> ExistsAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<MeetingParticipant>> GetByMeetingAsync(Guid meetingId, CancellationToken cancellationToken = default);
    Task<IEnumerable<MeetingParticipant>> GetByUserAsync(Guid authUserId, CancellationToken cancellationToken = default);
    Task<MeetingParticipant> AddAsync(MeetingParticipant entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(MeetingParticipant entity, CancellationToken cancellationToken = default);
}