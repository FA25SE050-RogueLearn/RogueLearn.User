using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.Roles.Commands.UpdateRole;

/// <summary>
/// Handles updating an existing Role.
/// - Loads role and throws standardized NotFoundException when missing.
/// - Emits structured logs for start and completion.
/// - Returns a typed response DTO.
/// </summary>
public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, UpdateRoleResponse>
{
  private readonly IRoleRepository _roleRepository;
  private readonly IMapper _mapper;
  private readonly ILogger<UpdateRoleCommandHandler> _logger;

  public UpdateRoleCommandHandler(IRoleRepository roleRepository, IMapper mapper, ILogger<UpdateRoleCommandHandler> logger)
  {
    _roleRepository = roleRepository;
    _mapper = mapper;
    _logger = logger;
  }

  /// <summary>
  /// Updates role properties and persists changes.
  /// </summary>
  public async Task<UpdateRoleResponse> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling UpdateRoleCommand for RoleId={RoleId}", request.Id);

    var role = await _roleRepository.GetByIdAsync(request.Id, cancellationToken);
    if (role == null)
    {
      _logger.LogWarning("Role not found: RoleId={RoleId}", request.Id);
      throw new NotFoundException("Role", request.Id);
    }

    role.Name = request.Name;
    role.Description = request.Description;

    var updatedRole = await _roleRepository.UpdateAsync(role, cancellationToken);
    _logger.LogInformation("Updated role: RoleId={RoleId}, Name={Name}", updatedRole.Id, updatedRole.Name);
    return _mapper.Map<UpdateRoleResponse>(updatedRole);
  }
}