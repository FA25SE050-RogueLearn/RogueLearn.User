using MediatR;

namespace RogueLearn.User.Application.Features.Subjects.Commands.DeleteSubject;

public class DeleteSubjectCommand : IRequest
{
    public Guid Id { get; set; }
}