using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.UserRoles.Queries.GetUserRoles;

public class GetUserRolesQueryHandler : IRequestHandler<GetUserRolesQuery, GetUserRolesResponse>
{
  private readonly IUserRoleRepository _userRoleRepository;
  private readonly IUserProfileRepository _userProfileRepository;
  private readonly IRoleRepository _roleRepository;
  private readonly IMapper _mapper;
  private readonly ILogger<GetUserRolesQueryHandler> _logger;

  public GetUserRolesQueryHandler(
    IUserRoleRepository userRoleRepository,
    IUserProfileRepository userProfileRepository,
    IRoleRepository roleRepository,
    IMapper mapper,
    ILogger<GetUserRolesQueryHandler> logger)
  {
    _userRoleRepository = userRoleRepository;
    _userProfileRepository = userProfileRepository;
    _roleRepository = roleRepository;
    _mapper = mapper;
    _logger = logger;
  }

  public async Task<GetUserRolesResponse> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
  {
    // Verify user exists
    var user = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);
    if (user == null)
    {
      throw new NotFoundException("User", request.UserId);
    }

    // Get user roles
    var userRoles = await _userRoleRepository.GetRolesForUserAsync(user.AuthUserId, cancellationToken);

    var userRoleDtos = new List<UserRoleDto>();

    // Map roles to DTOs
    foreach (var userRole in userRoles)
    {
      var role = await _roleRepository.GetByIdAsync(userRole.RoleId, cancellationToken);
      if (role != null)
      {
        var dto = _mapper.Map<UserRoleDto>(userRole);
        dto.RoleName = role.Name;
        dto.Description = role.Description;
        userRoleDtos.Add(dto);
      }
    }

    _logger.LogInformation("Retrieved {RoleCount} roles for user {UserId}", userRoleDtos.Count, request.UserId);

    return new GetUserRolesResponse
    {
      UserId = request.UserId,
      Roles = userRoleDtos
    };
  }
}