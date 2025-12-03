using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Notifications.Commands.MarkAsRead;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Unit>
{
    private readonly INotificationRepository _notificationRepository;

    public MarkNotificationReadCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<Unit> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var entity = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken)
            ?? throw new NotFoundException("Notification", request.NotificationId.ToString());

        if (entity.AuthUserId != request.AuthUserId)
        {
            throw new ForbiddenException("Cannot modify notifications that do not belong to the authenticated user.");
        }

        if (!entity.IsRead)
        {
            entity.IsRead = true;
            entity.ReadAt = DateTimeOffset.UtcNow;
            await _notificationRepository.UpdateAsync(entity, cancellationToken);
        }

        return Unit.Value;
    }
}