using MediatR;

namespace RogueLearn.User.Application.Features.Classes.Commands.SoftDeleteClass;

public record SoftDeleteClassCommand(Guid Id) : IRequest<Unit>;
