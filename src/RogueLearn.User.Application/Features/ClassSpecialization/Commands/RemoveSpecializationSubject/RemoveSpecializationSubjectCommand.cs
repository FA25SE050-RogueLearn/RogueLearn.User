using MediatR;

namespace RogueLearn.User.Application.Features.ClassSpecialization.Commands.RemoveSpecializationSubject;

public class RemoveSpecializationSubjectCommand : IRequest
{
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
}