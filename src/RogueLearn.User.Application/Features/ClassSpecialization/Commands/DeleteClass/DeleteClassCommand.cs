using MediatR;

namespace RogueLearn.User.Application.Features.ClassSpecialization.Commands.DeleteClass;

public class DeleteClassCommand : IRequest
{
    public Guid Id { get; set; }
}