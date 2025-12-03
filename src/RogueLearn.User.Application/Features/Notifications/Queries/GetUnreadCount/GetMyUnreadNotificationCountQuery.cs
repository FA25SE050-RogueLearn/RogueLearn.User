using MediatR;

namespace RogueLearn.User.Application.Features.Notifications.Queries.GetUnreadCount;

public record GetMyUnreadNotificationCountQuery(Guid AuthUserId) : IRequest<int>;