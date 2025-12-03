using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Infrastructure.Services;

public class LecturerNotificationService : ILecturerNotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserProfileRepository? _userProfileRepository;

    public LecturerNotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
        _userProfileRepository = null;
    }

    public LecturerNotificationService(INotificationRepository notificationRepository, IUserProfileRepository userProfileRepository)
    {
        _notificationRepository = notificationRepository;
        _userProfileRepository = userProfileRepository;
    }

    private async Task<string> GetUserNameAsync(Guid authUserId, CancellationToken cancellationToken)
    {
        if (_userProfileRepository == null) return string.Empty;
        var p = await _userProfileRepository.GetByAuthIdAsync(authUserId, cancellationToken);
        var first = p?.FirstName?.Trim();
        var last = p?.LastName?.Trim();
        var both = string.Join(" ", new[] { first, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return string.IsNullOrWhiteSpace(both) ? (p?.Username ?? string.Empty) : both;
    }
    

    public async Task NotifyRequestApprovedAsync(LecturerVerificationRequest request, CancellationToken cancellationToken = default)
    {
        var name = await GetUserNameAsync(request.AuthUserId, cancellationToken);
        await _notificationRepository.AddAsync(new Notification
        {
            AuthUserId = request.AuthUserId,
            Type = NotificationType.System,
            Title = "Lecturer verification approved",
            Message = "Your lecturer status has been approved.",
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["requestId"] = request.Id,
                ["userName"] = name
            }
        }, cancellationToken);
    }

    public async Task NotifyRequestDeclinedAsync(LecturerVerificationRequest request, CancellationToken cancellationToken = default)
    {
        var name = await GetUserNameAsync(request.AuthUserId, cancellationToken);
        await _notificationRepository.AddAsync(new Notification
        {
            AuthUserId = request.AuthUserId,
            Type = NotificationType.System,
            Title = "Lecturer verification declined",
            Message = "Your lecturer verification request was declined.",
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["requestId"] = request.Id,
                ["userName"] = name
            }
        }, cancellationToken);
    }
}