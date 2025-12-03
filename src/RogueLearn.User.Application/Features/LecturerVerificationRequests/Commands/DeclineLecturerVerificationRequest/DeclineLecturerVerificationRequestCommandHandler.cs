using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.LecturerVerificationRequests.Commands.DeclineLecturerVerificationRequest;

public class DeclineLecturerVerificationRequestCommandHandler : IRequestHandler<DeclineLecturerVerificationRequestCommand>
{
    private readonly ILecturerVerificationRequestRepository _requestRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ILogger<DeclineLecturerVerificationRequestCommandHandler> _logger;
    private readonly ILecturerNotificationService? _notificationService;

    public DeclineLecturerVerificationRequestCommandHandler(
        ILecturerVerificationRequestRepository requestRepository,
        IUserProfileRepository userProfileRepository,
        ILogger<DeclineLecturerVerificationRequestCommandHandler> logger,
        ILecturerNotificationService notificationService)
    {
        _requestRepository = requestRepository;
        _userProfileRepository = userProfileRepository;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(DeclineLecturerVerificationRequestCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new BadRequestException("Decline reason is required.");
        }

        var entity = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(LecturerVerificationRequest), request.RequestId);

        var profile = await _userProfileRepository.GetByAuthIdAsync(entity.AuthUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), entity.AuthUserId);

        var now = DateTimeOffset.UtcNow;

        entity.Status = VerificationStatus.Rejected;
        entity.ReviewNotes = request.Reason;
        entity.ReviewerId = request.ReviewerAuthUserId;
        entity.ReviewedAt = now;
        entity.UpdatedAt = now;
        await _requestRepository.UpdateAsync(entity, cancellationToken);

        // Decline is reflected on the request only; no profile status field.

        _logger.LogInformation("Declined lecturer verification request {RequestId} for {AuthUserId}", entity.Id, entity.AuthUserId);
        if (_notificationService != null)
        {
            await _notificationService.NotifyRequestDeclinedAsync(entity, cancellationToken);
        }
    }
}