using MediatR;

namespace RogueLearn.User.Application.Features.ClassNodes.Commands.MoveClassNode;

public record MoveClassNodeCommand(
    Guid ClassId,
    Guid NodeId,
    Guid? NewParentId,
    int NewSequence
) : IRequest<Unit>;
