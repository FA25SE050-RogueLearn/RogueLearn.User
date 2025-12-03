using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Notifications.Commands.Delete;

public class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand, Unit>
{
    private readonly INotificationRepository _notificationRepository;

    public DeleteNotificationCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<Unit> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
    {
        var entity = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken)
            ?? throw new NotFoundException("Notification", request.NotificationId.ToString());

        if (entity.AuthUserId != request.AuthUserId)
        {
            throw new ForbiddenException("Cannot delete notifications that do not belong to the authenticated user.");
        }

        await _notificationRepository.DeleteAsync(entity.Id, cancellationToken);
        return Unit.Value;
    }
}