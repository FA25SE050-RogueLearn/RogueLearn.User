using MediatR;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Features.ClassNodes.Commands.UpdateClassNode;

public record UpdateClassNodeCommand(
    Guid ClassId,
    Guid NodeId,
    string? Title,
    string? NodeType,
    string? Description,
    int? Sequence
) : IRequest<ClassNode>;
