using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Notifications.Commands.MarkAllRead;

public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, Unit>
{
    private readonly INotificationRepository _notificationRepository;

    public MarkAllNotificationsReadCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<Unit> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var items = await _notificationRepository.GetUnreadByUserAsync(request.AuthUserId, cancellationToken);
        var list = items.ToList();
        if (list.Count > 0)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var n in list)
            {
                n.IsRead = true;
                n.ReadAt = now;
            }
            await _notificationRepository.UpdateRangeAsync(list, cancellationToken);
        }
        return Unit.Value;
    }
}