using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;

/// <summary>
/// Handles retrieval of all Subjects.
/// Emits structured logs for observability and returns a list of SubjectDto.
/// </summary>
public class GetAllSubjectsHandler : IRequestHandler<GetAllSubjectsQuery, List<SubjectDto>>
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllSubjectsHandler> _logger;

    public GetAllSubjectsHandler(ISubjectRepository subjectRepository, IMapper mapper, ILogger<GetAllSubjectsHandler> logger)
    {
        _subjectRepository = subjectRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all subjects.
    /// </summary>
    public async Task<List<SubjectDto>> Handle(GetAllSubjectsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetAllSubjectsQuery");

        var subjects = await _subjectRepository.GetAllAsync(cancellationToken);
        var result = _mapper.Map<List<SubjectDto>>(subjects) ?? new List<SubjectDto>();

        _logger.LogInformation("Retrieved {Count} subjects", result.Count);
        return result;
    }
}