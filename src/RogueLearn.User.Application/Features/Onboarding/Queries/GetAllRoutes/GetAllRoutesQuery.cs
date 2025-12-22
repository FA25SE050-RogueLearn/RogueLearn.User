using MediatR;

namespace RogueLearn.User.Application.Features.Onboarding.Queries.GetAllRoutes;

public class GetAllRoutesQuery : IRequest<List<RouteDto>>
{
}