using MediatR;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Features.ClassNodes.Commands.CreateClassNode;

public record CreateClassNodeCommand(
    Guid ClassId,
    string Title,
    string? NodeType,
    string? Description,
    Guid? ParentId,
    int? Sequence
) : IRequest<ClassNode>;
