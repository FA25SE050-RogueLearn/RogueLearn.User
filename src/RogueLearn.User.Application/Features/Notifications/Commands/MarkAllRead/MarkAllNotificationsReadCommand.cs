using MediatR;

namespace RogueLearn.User.Application.Features.Notifications.Commands.MarkAllRead;

public record MarkAllNotificationsReadCommand(Guid AuthUserId) : IRequest<Unit>;