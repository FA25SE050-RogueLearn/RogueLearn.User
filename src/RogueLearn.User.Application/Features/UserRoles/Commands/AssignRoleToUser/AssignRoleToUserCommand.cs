using MediatR;

namespace RogueLearn.User.Application.Features.UserRoles.Commands.AssignRoleToUser;

public class AssignRoleToUserCommand : IRequest
{
    public Guid AuthUserId { get; set; }
    public Guid RoleId { get; set; }
}