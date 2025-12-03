using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Notifications.Commands.DeleteBatch;

public class DeleteNotificationsBatchCommandHandler : IRequestHandler<DeleteNotificationsBatchCommand, Unit>
{
    private readonly INotificationRepository _notificationRepository;

    public DeleteNotificationsBatchCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<Unit> Handle(DeleteNotificationsBatchCommand request, CancellationToken cancellationToken)
    {
        if (request.NotificationIds == null || request.NotificationIds.Count == 0)
        {
            return Unit.Value;
        }

        foreach (var id in request.NotificationIds.Distinct())
        {
            var entity = await _notificationRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException("Notification", id.ToString());

            if (entity.AuthUserId != request.AuthUserId)
            {
                throw new ForbiddenException("Cannot delete notifications that do not belong to the authenticated user.");
            }

            await _notificationRepository.DeleteAsync(entity.Id, cancellationToken);
        }

        return Unit.Value;
    }
}