using MediatR;

namespace RogueLearn.User.Application.Features.Onboarding.Queries.GetAllClasses;

public class GetAllClassesQuery : IRequest<List<ClassDto>>
{
}