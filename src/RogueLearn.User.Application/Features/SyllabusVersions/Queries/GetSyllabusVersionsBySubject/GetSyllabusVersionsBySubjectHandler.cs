using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.SyllabusVersions.Queries.GetSyllabusVersionsBySubject;

/// <summary>
/// Handles retrieval of syllabus versions for a given subject, ordered by version number descending.
/// Adds structured logging and ensures null-safe mapping.
/// </summary>
public class GetSyllabusVersionsBySubjectHandler : IRequestHandler<GetSyllabusVersionsBySubjectQuery, List<SyllabusVersionDto>>
{
    private readonly ISyllabusVersionRepository _syllabusVersionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetSyllabusVersionsBySubjectHandler> _logger;

    public GetSyllabusVersionsBySubjectHandler(
        ISyllabusVersionRepository syllabusVersionRepository,
        IMapper mapper,
        ILogger<GetSyllabusVersionsBySubjectHandler> logger)
    {
        _syllabusVersionRepository = syllabusVersionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves syllabus versions for the specified subject id and maps them to DTOs.
    /// </summary>
    /// <param name="request">The query request containing the subject id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of syllabus version DTOs ordered by version number descending.</returns>
    public async Task<List<SyllabusVersionDto>> Handle(GetSyllabusVersionsBySubjectQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {Handler} - retrieving syllabus versions for SubjectId={SubjectId}", nameof(GetSyllabusVersionsBySubjectHandler), request.SubjectId);

        var syllabusVersions = await _syllabusVersionRepository.FindAsync(
            sv => sv.SubjectId == request.SubjectId,
            cancellationToken);

        var ordered = syllabusVersions.OrderByDescending(sv => sv.VersionNumber).ToList();
        var dtos = _mapper.Map<List<SyllabusVersionDto>>(ordered) ?? new List<SyllabusVersionDto>();

        _logger.LogInformation("{Handler} - returning {Count} syllabus versions for SubjectId={SubjectId}", nameof(GetSyllabusVersionsBySubjectHandler), dtos.Count, request.SubjectId);

        return dtos;
    }
}