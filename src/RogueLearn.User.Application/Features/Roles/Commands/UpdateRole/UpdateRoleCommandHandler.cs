using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Roles.Commands.UpdateRole;

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, UpdateRoleResponse>
{
  private readonly IRoleRepository _roleRepository;
  private readonly IMapper _mapper;

  public UpdateRoleCommandHandler(IRoleRepository roleRepository, IMapper mapper)
  {
    _roleRepository = roleRepository;
    _mapper = mapper;
  }

  public async Task<UpdateRoleResponse> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
  {
    var role = await _roleRepository.GetByIdAsync(request.Id);
    if (role == null)
    {
      throw new NotFoundException("Role", request.Id);
    }

    role.Name = request.Name;
    role.Description = request.Description;

    var updatedRole = await _roleRepository.UpdateAsync(role);
    return _mapper.Map<UpdateRoleResponse>(updatedRole);
  }
}