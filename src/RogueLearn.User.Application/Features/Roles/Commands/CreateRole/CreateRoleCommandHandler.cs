using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Roles.Commands.CreateRole;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, CreateRoleResponse>
{
  private readonly IRoleRepository _roleRepository;
  private readonly IMapper _mapper;

  public CreateRoleCommandHandler(IRoleRepository roleRepository, IMapper mapper)
  {
    _roleRepository = roleRepository;
    _mapper = mapper;
  }

  public async Task<CreateRoleResponse> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
  {
    var role = new Role
    {
      Name = request.Name,
      Description = request.Description
    };

    var createdRole = await _roleRepository.AddAsync(role, cancellationToken);
    return _mapper.Map<CreateRoleResponse>(createdRole);
  }
}