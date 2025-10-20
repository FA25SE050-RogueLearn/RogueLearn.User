using MediatR;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Features.ClassNodes.Queries.GetFlatClassNodes;

public record GetFlatClassNodesQuery(Guid ClassId, bool OnlyActive) : IRequest<IReadOnlyList<ClassNode>>;
