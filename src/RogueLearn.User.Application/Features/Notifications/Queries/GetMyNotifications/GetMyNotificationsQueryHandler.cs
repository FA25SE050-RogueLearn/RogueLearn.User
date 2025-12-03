using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Notifications.Queries.GetMyNotifications;

public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, IReadOnlyList<Notification>>
{
    private readonly INotificationRepository _notificationRepository;

    public GetMyNotificationsQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<IReadOnlyList<Notification>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var size = request.Size <= 0 ? 20 : Math.Min(request.Size, 100);
        var items = await _notificationRepository.GetLatestByUserAsync(request.AuthUserId, size, cancellationToken);
        return items.ToList();
    }
}