using MediatR;

namespace RogueLearn.User.Application.Features.Roles.Commands.DeleteRole;

public class DeleteRoleCommand : IRequest
{
    public Guid Id { get; set; }
}