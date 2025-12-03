using MediatR;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Features.Notifications.Queries.GetMyNotifications;

public record GetMyNotificationsQuery(Guid AuthUserId, int Size) : IRequest<IReadOnlyList<Notification>>;