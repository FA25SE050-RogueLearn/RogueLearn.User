using MediatR;

namespace RogueLearn.User.Application.Features.SyllabusVersions.Commands.DeleteSyllabusVersion;

public record DeleteSyllabusVersionCommand(Guid Id) : IRequest;