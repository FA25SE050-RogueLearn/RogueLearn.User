using MediatR;

namespace RogueLearn.User.Application.Features.Roles.Commands.CreateRole;

public class CreateRoleCommand : IRequest<CreateRoleResponse>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}