using MediatR;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;

namespace RogueLearn.User.Application.Features.Subjects.Commands.DeleteSubject;

/// <summary>
/// Handles deletion of an existing Subject.
/// - Loads subject and throws standardized NotFoundException when missing.
/// - Emits structured logs for traceability.
/// </summary>
public class DeleteSubjectHandler : IRequestHandler<DeleteSubjectCommand>
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<DeleteSubjectHandler> _logger;

    public DeleteSubjectHandler(ISubjectRepository subjectRepository, ILogger<DeleteSubjectHandler> logger)
    {
        _subjectRepository = subjectRepository;
        _logger = logger;
    }

    /// <summary>
    /// Deletes a subject by Id.
    /// </summary>
    public async Task Handle(DeleteSubjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling DeleteSubjectCommand for SubjectId={SubjectId}", request.Id);

        var subject = await _subjectRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (subject == null)
        {
            _logger.LogWarning("Subject not found: SubjectId={SubjectId}", request.Id);
            throw new NotFoundException("Subject", request.Id);
        }

        await _subjectRepository.DeleteAsync(request.Id, cancellationToken);
        _logger.LogInformation("Deleted subject: SubjectId={SubjectId}", request.Id);
    }
}