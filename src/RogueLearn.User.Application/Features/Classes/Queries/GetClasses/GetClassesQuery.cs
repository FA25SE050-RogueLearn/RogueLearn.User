using MediatR;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Features.Classes.Queries.GetClasses;

public record GetClassesQuery(bool? Active) : IRequest<IReadOnlyList<Class>>;
