using MediatR;

namespace RogueLearn.User.Application.Features.Notifications.Commands.DeleteBatch;

public record DeleteNotificationsBatchCommand(IReadOnlyList<Guid> NotificationIds, Guid AuthUserId) : IRequest<Unit>;