using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.LecturerVerificationRequests.Commands.ApproveLecturerVerificationRequest;

public class ApproveLecturerVerificationRequestCommandHandler : IRequestHandler<ApproveLecturerVerificationRequestCommand>
{
    private readonly ILecturerVerificationRequestRepository _requestRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IGuildRepository _guildRepository;
    private readonly ILogger<ApproveLecturerVerificationRequestCommandHandler> _logger;

    public ApproveLecturerVerificationRequestCommandHandler(
        ILecturerVerificationRequestRepository requestRepository,
        IUserProfileRepository userProfileRepository,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IGuildRepository guildRepository,
        ILogger<ApproveLecturerVerificationRequestCommandHandler> logger)
    {
        _requestRepository = requestRepository;
        _userProfileRepository = userProfileRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _guildRepository = guildRepository;
        _logger = logger;
    }

    public async Task Handle(ApproveLecturerVerificationRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _requestRepository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(LecturerVerificationRequest), request.RequestId);

        var profile = await _userProfileRepository.GetByAuthIdAsync(entity.AuthUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), entity.AuthUserId);

        var now = DateTimeOffset.UtcNow;

        var alreadyApproved = entity.Status == VerificationStatus.Approved;
        entity.Status = VerificationStatus.Approved;
        entity.ReviewNotes = request.Note;
        entity.ReviewerId = request.ReviewerAuthUserId;
        entity.ReviewedAt = now;
        entity.UpdatedAt = now;
        await _requestRepository.UpdateAsync(entity, cancellationToken);

        // Approval is reflected via role assignment only.

        var role = await _roleRepository.GetByNameAsync("Verified Lecturer", cancellationToken)
            ?? throw new NotFoundException(nameof(Role), "Verified Lecturer");
        var existingUserRoles = await _userRoleRepository.GetRolesForUserAsync(profile.AuthUserId, cancellationToken);
        if (!existingUserRoles.Any(ur => ur.RoleId == role.Id))
        {
            await _userRoleRepository.AddAsync(new UserRole
            {
                Id = Guid.NewGuid(),
                AuthUserId = profile.AuthUserId,
                RoleId = role.Id,
                AssignedAt = now,
                AssignedBy = request.ReviewerAuthUserId
            }, cancellationToken);
        }

        var createdGuilds = await _guildRepository.GetGuildsByCreatorAsync(profile.AuthUserId, cancellationToken);
        foreach (var g in createdGuilds)
        {
            if (!g.IsLecturerGuild)
            {
                g.IsLecturerGuild = true;
                g.UpdatedAt = now;
                await _guildRepository.UpdateAsync(g, cancellationToken);
            }
        }

        _logger.LogInformation("Approved lecturer verification request {RequestId} for {AuthUserId}", entity.Id, entity.AuthUserId);
    }
}