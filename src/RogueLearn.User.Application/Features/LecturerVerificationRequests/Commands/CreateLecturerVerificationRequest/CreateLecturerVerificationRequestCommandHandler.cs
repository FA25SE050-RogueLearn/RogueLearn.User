using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.LecturerVerificationRequests.Commands.CreateLecturerVerificationRequest;

public class CreateLecturerVerificationRequestCommandHandler : IRequestHandler<CreateLecturerVerificationRequestCommand, CreateLecturerVerificationRequestResponse>
{
    private readonly ILecturerVerificationRequestRepository _requestRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ILogger<CreateLecturerVerificationRequestCommandHandler> _logger;
    private readonly ILecturerNotificationService? _notificationService;

    public CreateLecturerVerificationRequestCommandHandler(
        ILecturerVerificationRequestRepository requestRepository,
        IUserProfileRepository userProfileRepository,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        ILogger<CreateLecturerVerificationRequestCommandHandler> logger,
        ILecturerNotificationService notificationService)
    {
        _requestRepository = requestRepository;
        _userProfileRepository = userProfileRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<CreateLecturerVerificationRequestResponse> Handle(CreateLecturerVerificationRequestCommand request, CancellationToken cancellationToken)
    {
        var profile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), request.AuthUserId);

        if (!string.Equals(profile.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Email does not match authenticated account.");
        }

        var hasPending = await _requestRepository.AnyPendingAsync(request.AuthUserId, cancellationToken);
        if (hasPending)
        {
            throw new ConflictException("You already have a pending verification request.");
        }

        var approvedRequests = await _requestRepository.FindAsync(r => r.AuthUserId == request.AuthUserId && r.Status == VerificationStatus.Approved, cancellationToken);
        if (approvedRequests.Any(r => r.ReviewedAt == null))
        {
            throw new ConflictException("You already have an approved verification request.");
        }

        var lecturerRole = await _roleRepository.GetByNameAsync("Verified Lecturer", cancellationToken);
        if (lecturerRole != null)
        {
            var userRoles = await _userRoleRepository.GetRolesForUserAsync(request.AuthUserId, cancellationToken);
            if (userRoles.Any(ur => ur.RoleId == lecturerRole.Id))
            {
                throw new ConflictException("You already have an approved verification request.");
            }
        }

        var entity = new LecturerVerificationRequest
        {
            Id = Guid.NewGuid(),
            AuthUserId = request.AuthUserId,
            Documents = new Dictionary<string, object>
            {
                ["email"] = request.Email,
                ["staffId"] = request.StaffId
            },
            Status = VerificationStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(request.ScreenshotUrl))
        {
            entity.Documents!["screenshotUrl"] = request.ScreenshotUrl;
        }

        await _requestRepository.AddAsync(entity, cancellationToken);

        _logger.LogInformation("Lecturer verification request created: {RequestId} for {AuthUserId}", entity.Id, request.AuthUserId);

        return new CreateLecturerVerificationRequestResponse
        {
            RequestId = entity.Id,
            Status = "pending"
        };
    }
}