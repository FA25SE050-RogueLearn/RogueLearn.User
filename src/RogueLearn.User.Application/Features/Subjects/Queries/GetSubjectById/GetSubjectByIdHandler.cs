using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.Subjects.Queries.GetSubjectById;

/// <summary>
/// Handles retrieval of a Subject by its identifier.
/// Returns null when the subject does not exist and emits structured logs for observability.
/// </summary>
public class GetSubjectByIdHandler : IRequestHandler<GetSubjectByIdQuery, SubjectDto?>
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetSubjectByIdHandler> _logger;

    public GetSubjectByIdHandler(ISubjectRepository subjectRepository, IMapper mapper, ILogger<GetSubjectByIdHandler> logger)
    {
        _subjectRepository = subjectRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a subject by Id. Returns null if not found.
    /// </summary>
    public async Task<SubjectDto?> Handle(GetSubjectByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetSubjectByIdQuery for SubjectId={SubjectId}", request.Id);

        var subject = await _subjectRepository.GetByIdAsync(request.Id, cancellationToken);
        if (subject == null)
        {
            _logger.LogInformation("Subject not found: SubjectId={SubjectId}", request.Id);
            return null;
        }

        var dto = _mapper.Map<SubjectDto>(subject);
        _logger.LogInformation("Subject retrieved: SubjectId={SubjectId}, SubjectCode={SubjectCode}", subject.Id, subject.SubjectCode);
        return dto;
    }
}