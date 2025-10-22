using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;

namespace RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubject;

/// <summary>
/// Handles updating an existing Subject.
/// - Loads subject and throws standardized NotFoundException when missing.
/// - Emits structured logs for traceability.
/// - Returns a response DTO.
/// </summary>
public class UpdateSubjectHandler : IRequestHandler<UpdateSubjectCommand, UpdateSubjectResponse>
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateSubjectHandler> _logger;

    public UpdateSubjectHandler(ISubjectRepository subjectRepository, IMapper mapper, ILogger<UpdateSubjectHandler> logger)
    {
        _subjectRepository = subjectRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Updates subject fields and persists changes.
    /// </summary>
    public async Task<UpdateSubjectResponse> Handle(UpdateSubjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling UpdateSubjectCommand for SubjectId={SubjectId}", request.Id);

        var subject = await _subjectRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (subject == null)
        {
            _logger.LogWarning("Subject not found: SubjectId={SubjectId}", request.Id);
            throw new NotFoundException("Subject", request.Id);
        }

        subject.SubjectCode = request.SubjectCode;
        subject.SubjectName = request.SubjectName;
        subject.Credits = request.Credits;
        subject.Description = request.Description;
        subject.UpdatedAt = DateTimeOffset.UtcNow;

        var updatedSubject = await _subjectRepository.UpdateAsync(subject, cancellationToken);
        _logger.LogInformation("Updated subject: SubjectId={SubjectId}, SubjectCode={SubjectCode}", updatedSubject.Id, updatedSubject.SubjectCode);
        return _mapper.Map<UpdateSubjectResponse>(updatedSubject);
    }
}