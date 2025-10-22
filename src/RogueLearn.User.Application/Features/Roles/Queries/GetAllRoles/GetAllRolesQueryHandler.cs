using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.Roles.Queries.GetAllRoles;

/// <summary>
/// Handles retrieval of all Roles.
/// Emits structured logs for observability and returns a response DTO containing role list.
/// </summary>
public class GetAllRolesQueryHandler : IRequestHandler<GetAllRolesQuery, GetAllRolesResponse>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllRolesQueryHandler> _logger;

    public GetAllRolesQueryHandler(IRoleRepository roleRepository, IMapper mapper, ILogger<GetAllRolesQueryHandler> logger)
    {
        _roleRepository = roleRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all roles and maps them into DTOs.
    /// </summary>
    public async Task<GetAllRolesResponse> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetAllRolesQuery");

        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        var roleDtos = _mapper.Map<List<RoleDto>>(roles) ?? new List<RoleDto>();

        _logger.LogInformation("Retrieved {Count} roles", roleDtos.Count);
        return new GetAllRolesResponse
        {
            Roles = roleDtos
        };
    }
}