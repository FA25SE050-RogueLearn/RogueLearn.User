using MediatR;

namespace RogueLearn.User.Application.Features.Notifications.Commands.MarkAsRead;

public record MarkNotificationReadCommand(Guid NotificationId, Guid AuthUserId) : IRequest<Unit>;