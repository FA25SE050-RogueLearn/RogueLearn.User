using MediatR;

namespace RogueLearn.User.Application.Features.UserRoles.Commands.RemoveRoleFromUser;

public class RemoveRoleFromUserCommand : IRequest
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}