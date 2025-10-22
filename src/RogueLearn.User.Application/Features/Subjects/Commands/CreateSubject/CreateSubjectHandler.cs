using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;

/// <summary>
/// Handles creation of a new Subject.
/// - Sets audit fields in handler.
/// - Emits structured logs for observability.
/// - Returns a response DTO via AutoMapper.
/// </summary>
public class CreateSubjectHandler : IRequestHandler<CreateSubjectCommand, CreateSubjectResponse>
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateSubjectHandler> _logger;

    public CreateSubjectHandler(ISubjectRepository subjectRepository, IMapper mapper, ILogger<CreateSubjectHandler> logger)
    {
        _subjectRepository = subjectRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Creates a subject and persists it.
    /// </summary>
    public async Task<CreateSubjectResponse> Handle(CreateSubjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling CreateSubjectCommand for SubjectCode={SubjectCode}, SubjectName={SubjectName}", request.SubjectCode, request.SubjectName);

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            SubjectCode = request.SubjectCode,
            SubjectName = request.SubjectName,
            Credits = request.Credits,
            Description = request.Description,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var createdSubject = await _subjectRepository.AddAsync(subject, cancellationToken);
        _logger.LogInformation("Created subject: SubjectId={SubjectId}, SubjectCode={SubjectCode}", createdSubject.Id, createdSubject.SubjectCode);
        return _mapper.Map<CreateSubjectResponse>(createdSubject);
    }
}