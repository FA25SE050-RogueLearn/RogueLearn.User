using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.ClassNodes.Queries.GetTreeClassNodes;

public record GetTreeClassNodesQuery(Guid ClassId, bool OnlyActive) : IRequest<IReadOnlyList<ClassNodeTreeItem>>;
