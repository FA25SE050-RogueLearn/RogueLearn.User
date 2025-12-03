using MediatR;

namespace RogueLearn.User.Application.Features.Notifications.Commands.Delete;

public record DeleteNotificationCommand(Guid NotificationId, Guid AuthUserId) : IRequest<Unit>;