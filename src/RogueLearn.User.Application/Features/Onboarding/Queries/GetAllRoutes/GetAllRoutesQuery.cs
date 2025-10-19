// RogueLearn.User/src/RogueLearn.User.Application/Features/Onboarding/Queries/GetAllRoutes/GetAllRoutesQuery.cs
using MediatR;

namespace RogueLearn.User.Application.Features.Onboarding.Queries.GetAllRoutes;

public class GetAllRoutesQuery : IRequest<List<RouteDto>>
{
}