// RogueLearn.User/src/RogueLearn.User.Application/Features/Onboarding/Queries/GetAllClasses/GetAllClassesQuery.cs
using MediatR;

namespace RogueLearn.User.Application.Features.Onboarding.Queries.GetAllClasses;

public class GetAllClassesQuery : IRequest<List<ClassDto>>
{
}