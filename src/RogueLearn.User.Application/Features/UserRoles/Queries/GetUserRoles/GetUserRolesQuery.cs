using MediatR;

namespace RogueLearn.User.Application.Features.UserRoles.Queries.GetUserRoles;

public class GetUserRolesQuery : IRequest<GetUserRolesResponse>
{
    public Guid AuthUserId { get; set; }
}