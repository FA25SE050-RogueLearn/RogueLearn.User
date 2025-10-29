using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.CurriculumStructure.Queries.GetCurriculumStructureByVersion;

/// <summary>
/// Handles retrieval of curriculum structure entries for a specific curriculum version.
/// Emits structured logs for observability and returns a list of CurriculumStructureDto ordered by term and subject code.
/// </summary>
public class GetCurriculumStructureByVersionHandler : IRequestHandler<GetCurriculumStructureByVersionQuery, List<CurriculumStructureDto>>
{
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCurriculumStructureByVersionHandler> _logger;

    public GetCurriculumStructureByVersionHandler(
        ICurriculumStructureRepository curriculumStructureRepository,
        ISubjectRepository subjectRepository,
        IMapper mapper,
        ILogger<GetCurriculumStructureByVersionHandler> logger)
    {
        _curriculumStructureRepository = curriculumStructureRepository;
        _subjectRepository = subjectRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves curriculum structure for the provided version identifier.
    /// </summary>
    public async Task<List<CurriculumStructureDto>> Handle(GetCurriculumStructureByVersionQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetCurriculumStructureByVersionQuery for CurriculumVersionId={CurriculumVersionId}", request.CurriculumVersionId);

        var structures = await _curriculumStructureRepository.GetAllAsync(cancellationToken);
        var versionStructures = structures.Where(s => s.CurriculumVersionId == request.CurriculumVersionId).ToList();
        _logger.LogInformation("Found {Count} curriculum structures for version {CurriculumVersionId}", versionStructures.Count, request.CurriculumVersionId);

        var result = new List<CurriculumStructureDto>();

        foreach (var structure in versionStructures)
        {
            var subject = await _subjectRepository.GetByIdAsync(structure.SubjectId, cancellationToken);

            var dto = new CurriculumStructureDto
            {
                Id = structure.Id,
                CurriculumVersionId = structure.CurriculumVersionId,
                SubjectId = structure.SubjectId,
                SubjectCode = subject?.SubjectCode ?? string.Empty,
                SubjectName = subject?.SubjectName ?? string.Empty,
                Credits = subject?.Credits ?? 0,
                TermNumber = structure.Semester,
                IsMandatory = structure.IsMandatory,
                PrerequisiteSubjectIds = structure.PrerequisiteSubjectIds,
                PrerequisitesText = structure.PrerequisitesText,
                CreatedAt = structure.CreatedAt
            };

            result.Add(dto);
        }

        var ordered = result.OrderBy(r => r.TermNumber).ThenBy(r => r.SubjectCode).ToList();
        _logger.LogInformation("Returning {Count} curriculum structure entries for version {CurriculumVersionId}", ordered.Count, request.CurriculumVersionId);
        return ordered;
    }
}