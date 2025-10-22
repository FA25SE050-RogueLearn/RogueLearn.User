using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Roles.Commands.DeleteRole;

/// <summary>
/// Handles deletion of an existing Role.
/// - Loads role and throws standardized NotFoundException when missing.
/// - Prevents deletion if role is assigned to users and throws BadRequestException.
/// - Emits structured logs for start and completion.
/// </summary>
public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ILogger<DeleteRoleCommandHandler> _logger;

    public DeleteRoleCommandHandler(
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        ILogger<DeleteRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _logger = logger;
    }

    /// <summary>
    /// Deletes a role by Id. Prevents deletion when assigned to any users.
    /// </summary>
    public async Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling DeleteRoleCommand for RoleId={RoleId}", request.Id);

        // Check if role exists
        var role = await _roleRepository.GetByIdAsync(request.Id, cancellationToken);
        if (role == null)
        {
            _logger.LogWarning("Role not found: RoleId={RoleId}", request.Id);
            throw new NotFoundException("Role", request.Id);
        }

        // Check if role is assigned to any users
        var userRoles = await _userRoleRepository.GetUsersByRoleIdAsync(request.Id, cancellationToken);
        if (userRoles.Any())
        {
            _logger.LogInformation("Delete prevented: RoleId={RoleId} assigned to {UserCount} users", role.Id, userRoles.Count());
            throw new BadRequestException($"Cannot delete role '{role.Name}' because it is assigned to {userRoles.Count()} user(s). Remove the role from all users first.");
        }

        // Delete the role
        await _roleRepository.DeleteAsync(request.Id, cancellationToken);

        _logger.LogInformation("Role '{RoleName}' with ID {RoleId} deleted", role.Name, role.Id);
    }
}