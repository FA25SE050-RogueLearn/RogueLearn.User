namespace RogueLearn.User.Application.Interfaces;

public interface ILecturerNotificationService
{
    Task NotifyRequestApprovedAsync(RogueLearn.User.Domain.Entities.LecturerVerificationRequest request, CancellationToken cancellationToken = default);
    Task NotifyRequestDeclinedAsync(RogueLearn.User.Domain.Entities.LecturerVerificationRequest request, CancellationToken cancellationToken = default);
}