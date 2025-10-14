using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Subjects.Commands.DeleteSubject;

public class DeleteSubjectHandler : IRequestHandler<DeleteSubjectCommand>
{
    private readonly ISubjectRepository _subjectRepository;

    public DeleteSubjectHandler(ISubjectRepository subjectRepository)
    {
        _subjectRepository = subjectRepository;
    }

    public async Task Handle(DeleteSubjectCommand request, CancellationToken cancellationToken)
    {
        var subject = await _subjectRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (subject == null)
            throw new ArgumentException($"Subject with ID {request.Id} not found.");

        await _subjectRepository.DeleteAsync(request.Id, cancellationToken);
    }
}