using MediatR;

namespace RogueLearn.User.Application.Features.Roles.Queries.GetAllRoles;

public class GetAllRolesQuery : IRequest<GetAllRolesResponse>
{
    // No parameters needed for getting all roles
}