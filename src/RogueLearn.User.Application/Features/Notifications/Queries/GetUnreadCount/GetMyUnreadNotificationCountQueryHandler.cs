using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Notifications.Queries.GetUnreadCount;

public class GetMyUnreadNotificationCountQueryHandler : IRequestHandler<GetMyUnreadNotificationCountQuery, int>
{
    private readonly INotificationRepository _notificationRepository;

    public GetMyUnreadNotificationCountQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<int> Handle(GetMyUnreadNotificationCountQuery request, CancellationToken cancellationToken)
    {
        return await _notificationRepository.CountUnreadByUserAsync(request.AuthUserId, cancellationToken);
    }
}