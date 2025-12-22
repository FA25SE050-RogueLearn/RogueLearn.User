using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.ClassSpecialization.Commands.RemoveSpecializationSubject;

public class RemoveSpecializationSubjectCommandHandler : IRequestHandler<RemoveSpecializationSubjectCommand>
{
    private readonly IClassSpecializationSubjectRepository _repository;

    public RemoveSpecializationSubjectCommandHandler(IClassSpecializationSubjectRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(RemoveSpecializationSubjectCommand request, CancellationToken cancellationToken)
    {
        var mapping = await _repository.FirstOrDefaultAsync(
            m => m.ClassId == request.ClassId && m.SubjectId == request.SubjectId,
            cancellationToken);

        if (mapping == null)
        {
            throw new NotFoundException("Specialization subject mapping not found.");
        }

        await _repository.DeleteAsync(mapping.Id, cancellationToken);
    }
}