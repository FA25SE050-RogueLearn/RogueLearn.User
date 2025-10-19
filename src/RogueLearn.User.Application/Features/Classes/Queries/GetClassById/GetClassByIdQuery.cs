using MediatR;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Features.Classes.Queries.GetClassById;

public record GetClassByIdQuery(Guid Id) : IRequest<Class?>;
