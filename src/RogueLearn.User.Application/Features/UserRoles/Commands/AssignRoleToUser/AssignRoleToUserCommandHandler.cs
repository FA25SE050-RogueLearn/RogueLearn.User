using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.UserRoles.Commands.AssignRoleToUser;

public class AssignRoleToUserCommandHandler : IRequestHandler<AssignRoleToUserCommand>
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<AssignRoleToUserCommandHandler> _logger;

    public AssignRoleToUserCommandHandler(
        IUserRoleRepository userRoleRepository,
        IUserProfileRepository userProfileRepository,
        IRoleRepository roleRepository,
        ILogger<AssignRoleToUserCommandHandler> logger)
    {
        _userRoleRepository = userRoleRepository;
        _userProfileRepository = userProfileRepository;
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
    {
        // Verify user exists
        var user = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User", request.UserId);
        }

        // Verify role exists
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
        {
            throw new NotFoundException("Role", request.RoleId);
        }

        // Check if user already has this role
        var existingUserRoles = await _userRoleRepository.GetRolesForUserAsync(user.AuthUserId, cancellationToken);
        if (existingUserRoles.Any(ur => ur.RoleId == request.RoleId))
        {
            throw new BadRequestException($"User already has the role '{role.Name}'.");
        }

        // Create new user role assignment
        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            AuthUserId = user.AuthUserId,
            RoleId = request.RoleId,
            AssignedAt = DateTimeOffset.UtcNow
        };

        await _userRoleRepository.AddAsync(userRole, cancellationToken);

        _logger.LogInformation("Role '{RoleName}' assigned to user '{Username}' (AuthUserId: {AuthUserId})",
            role.Name, user.Username, user.AuthUserId);
    }
}