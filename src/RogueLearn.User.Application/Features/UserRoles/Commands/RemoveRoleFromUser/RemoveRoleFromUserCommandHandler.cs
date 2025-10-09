using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.UserRoles.Commands.RemoveRoleFromUser;

public class RemoveRoleFromUserCommandHandler : IRequestHandler<RemoveRoleFromUserCommand>
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<RemoveRoleFromUserCommandHandler> _logger;

    public RemoveRoleFromUserCommandHandler(
        IUserRoleRepository userRoleRepository,
        IUserProfileRepository userProfileRepository,
        IRoleRepository roleRepository,
        ILogger<RemoveRoleFromUserCommandHandler> logger)
    {
        _userRoleRepository = userRoleRepository;
        _userProfileRepository = userProfileRepository;
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task Handle(RemoveRoleFromUserCommand request, CancellationToken cancellationToken)
    {
        // Verify user exists
        var user = await _userProfileRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            throw new NotFoundException("User", request.UserId);
        }

        // Verify role exists
        var role = await _roleRepository.GetByIdAsync(request.RoleId);
        if (role == null)
        {
            throw new NotFoundException("Role", request.RoleId);
        }

        // Find the user role to remove
        var existingUserRoles = await _userRoleRepository.GetRolesForUserAsync(user.AuthUserId);
        var userRoleToRemove = existingUserRoles.FirstOrDefault(ur => ur.RoleId == request.RoleId);
        
        if (userRoleToRemove == null)
        {
            throw new BadRequestException($"User does not have the role '{role.Name}'.");
        }

        // Remove the user role
        await _userRoleRepository.DeleteAsync(userRoleToRemove.Id);

        _logger.LogInformation("Role '{RoleName}' removed from user {UserId}", role.Name, request.UserId);
    }
}