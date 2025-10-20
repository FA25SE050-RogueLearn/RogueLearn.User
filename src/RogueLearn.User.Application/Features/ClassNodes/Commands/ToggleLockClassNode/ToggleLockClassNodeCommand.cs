using MediatR;

namespace RogueLearn.User.Application.Features.ClassNodes.Commands.ToggleLockClassNode;

public record ToggleLockClassNodeCommand(
    Guid ClassId,
    Guid NodeId,
    bool IsLocked,
    string? Reason
) : IRequest<Unit>;
