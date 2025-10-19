using MediatR;

namespace RogueLearn.User.Application.Features.ClassNodes.Commands.SoftDeleteClassNode;

public record SoftDeleteClassNodeCommand(
    Guid ClassId,
    Guid NodeId
) : IRequest<Unit>;
