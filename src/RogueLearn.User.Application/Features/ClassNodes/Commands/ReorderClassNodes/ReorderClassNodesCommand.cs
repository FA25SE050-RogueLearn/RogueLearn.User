using MediatR;

namespace RogueLearn.User.Application.Features.ClassNodes.Commands.ReorderClassNodes;

public record ReorderClassNodesCommand(
    Guid ClassId,
    Guid? ParentId,
    IReadOnlyList<(Guid nodeId, int sequence)> Items
) : IRequest<Unit>;
