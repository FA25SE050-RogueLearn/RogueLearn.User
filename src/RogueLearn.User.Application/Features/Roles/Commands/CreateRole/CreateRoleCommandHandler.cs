using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.Roles.Commands.CreateRole;

/// <summary>
/// Handles creation of a new Role.
/// - Enforces uniqueness by role name.
/// - Emits structured logs for traceability.
/// - Returns a typed response DTO without leaking domain internals.
/// </summary>
public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, CreateRoleResponse>
{
  private readonly IRoleRepository _roleRepository;
  private readonly IMapper _mapper;
  private readonly ILogger<CreateRoleCommandHandler> _logger;

  public CreateRoleCommandHandler(IRoleRepository roleRepository, IMapper mapper, ILogger<CreateRoleCommandHandler> logger)
  {
    _roleRepository = roleRepository;
    _mapper = mapper;
    _logger = logger;
  }

  /// <summary>
  /// Creates a role after enforcing uniqueness by name.
  /// </summary>
  public async Task<CreateRoleResponse> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Handling CreateRoleCommand for Name={Name}", request.Name);

    // Prevent duplicate role names
    var existing = await _roleRepository.GetByNameAsync(request.Name, cancellationToken);
    if (existing != null)
    {
      _logger.LogInformation("Role with Name={Name} already exists", request.Name);
      throw new BadRequestException($"Role '{request.Name}' already exists.");
    }

    var role = new Role
    {
      Name = request.Name,
      Description = request.Description
    };

    var createdRole = await _roleRepository.AddAsync(role, cancellationToken);
    _logger.LogInformation("Created role: RoleId={RoleId}, Name={Name}", createdRole.Id, createdRole.Name);
    return _mapper.Map<CreateRoleResponse>(createdRole);
  }
}