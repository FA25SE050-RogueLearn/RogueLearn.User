using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface INotificationRepository : IGenericRepository<Notification>
{
    Task<IEnumerable<Notification>> GetLatestByUserAsync(Guid authUserId, int size = 20, CancellationToken cancellationToken = default);
    Task<int> CountUnreadByUserAsync(Guid authUserId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Notification>> GetUnreadByUserAsync(Guid authUserId, CancellationToken cancellationToken = default);
}