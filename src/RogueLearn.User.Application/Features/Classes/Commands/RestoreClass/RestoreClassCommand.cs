using MediatR;

namespace RogueLearn.User.Application.Features.Classes.Commands.RestoreClass;

public record RestoreClassCommand(Guid Id) : IRequest<Unit>;
