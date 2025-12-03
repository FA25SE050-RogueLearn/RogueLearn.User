using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Interfaces;

public interface IMeetingNotificationService
{
    Task NotifyMeetingScheduledAsync(Meeting meeting, CancellationToken cancellationToken = default);
}