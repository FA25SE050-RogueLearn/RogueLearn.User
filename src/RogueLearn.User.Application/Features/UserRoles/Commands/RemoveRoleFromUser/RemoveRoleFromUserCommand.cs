using MediatR;

namespace RogueLearn.User.Application.Features.UserRoles.Commands.RemoveRoleFromUser;

public class RemoveRoleFromUserCommand : IRequest
{
    public Guid AuthUserId { get; set; }
    public Guid RoleId { get; set; }
}