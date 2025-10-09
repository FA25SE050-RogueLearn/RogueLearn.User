using MediatR;

namespace RogueLearn.User.Application.Features.Roles.Commands.UpdateRole;

public class UpdateRoleCommand : IRequest<UpdateRoleResponse>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}