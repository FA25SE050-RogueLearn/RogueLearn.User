using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IMeetingSummaryRepository
{
    Task<MeetingSummary?> GetByMeetingAsync(Guid meetingId, CancellationToken cancellationToken = default);
    Task<MeetingSummary> AddAsync(MeetingSummary entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(MeetingSummary entity, CancellationToken cancellationToken = default);
}