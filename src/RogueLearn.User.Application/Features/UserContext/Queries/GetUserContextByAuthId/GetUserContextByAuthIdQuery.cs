using MediatR;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Features.UserContext.Queries.GetUserContextByAuthId;

public class GetUserContextByAuthIdQuery : IRequest<UserContextDto?>
{
    public Guid AuthId { get; set; }

    public GetUserContextByAuthIdQuery(Guid authId)
    {
        AuthId = authId;
    }
}