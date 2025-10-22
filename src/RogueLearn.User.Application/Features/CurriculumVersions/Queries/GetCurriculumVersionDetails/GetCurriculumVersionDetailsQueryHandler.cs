// RogueLearn.User/src/RogueLearn.User.Application/Features/CurriculumVersions/Queries/GetCurriculumVersionDetails/GetCurriculumVersionDetailsQueryHandler.cs
using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.CurriculumVersions.Queries.GetCurriculumVersionDetails;

public class GetCurriculumVersionDetailsQueryHandler : IRequestHandler<GetCurriculumVersionDetailsQuery, CurriculumVersionDetailsDto>
{
    private readonly ICurriculumVersionRepository _curriculumVersionRepository;
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ISyllabusVersionRepository _syllabusVersionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCurriculumVersionDetailsQueryHandler> _logger;

    public GetCurriculumVersionDetailsQueryHandler(
        ICurriculumVersionRepository curriculumVersionRepository,
        ICurriculumStructureRepository curriculumStructureRepository,
        ISubjectRepository subjectRepository,
        ISyllabusVersionRepository syllabusVersionRepository,
        IMapper mapper,
        ILogger<GetCurriculumVersionDetailsQueryHandler> logger)
    {
        _curriculumVersionRepository = curriculumVersionRepository;
        _curriculumStructureRepository = curriculumStructureRepository;
        _subjectRepository = subjectRepository;
        _syllabusVersionRepository = syllabusVersionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves curriculum version details including subjects and their syllabus versions, and computes analysis metrics.
    /// </summary>
    /// <param name="request">The request containing the CurriculumVersionId.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A fully populated CurriculumVersionDetailsDto with analysis.</returns>
    public async Task<CurriculumVersionDetailsDto> Handle(GetCurriculumVersionDetailsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching curriculum version details for versionId {VersionId}", request.CurriculumVersionId);

        // Get the curriculum version directly
        var version = await _curriculumVersionRepository.GetByIdAsync(request.CurriculumVersionId, cancellationToken);
        if (version == null)
        {
            _logger.LogWarning("CurriculumVersion not found for id {VersionId}", request.CurriculumVersionId);
            throw new NotFoundException("CurriculumVersion", request.CurriculumVersionId);
        }

        var versionDto = _mapper.Map<CurriculumVersionDetailsDto>(version);

        // Get curriculum structure (subjects) for this version
        var curriculumStructures = await _curriculumStructureRepository.FindAsync(
            cs => cs.CurriculumVersionId == version.Id,
            cancellationToken) ?? Enumerable.Empty<RogueLearn.User.Domain.Entities.CurriculumStructure>();

        var structureCount = curriculumStructures.Count();
        _logger.LogInformation("Found {StructureCount} curriculum structures for version {VersionId}", structureCount, version.Id);

        versionDto.Subjects = new List<CurriculumSubjectDetailsDto>();

        foreach (var structure in curriculumStructures.OrderBy(s => s.TermNumber).ThenBy(s => s.IsMandatory ? 0 : 1))
        {
            var subject = await _subjectRepository.GetByIdAsync(structure.SubjectId, cancellationToken);
            if (subject == null)
            {
                _logger.LogWarning("Subject id {SubjectId} referenced by curriculum structure not found (versionId {VersionId})", structure.SubjectId, version.Id);
                continue;
            }

            var subjectDto = new CurriculumSubjectDetailsDto
            {
                SubjectId = subject.Id,
                SubjectCode = subject.SubjectCode,
                SubjectName = subject.SubjectName,
                Credits = subject.Credits,
                Description = subject.Description,
                TermNumber = structure.TermNumber,
                IsMandatory = structure.IsMandatory,
                PrerequisiteSubjectIds = structure.PrerequisiteSubjectIds,
                PrerequisitesText = structure.PrerequisitesText
            };

            var syllabusVersions = await _syllabusVersionRepository.FindAsync(
                sv => sv.SubjectId == subject.Id,
                cancellationToken) ?? Enumerable.Empty<RogueLearn.User.Domain.Entities.SyllabusVersion>();

            subjectDto.SyllabusVersions = syllabusVersions
                .OrderByDescending(sv => sv.VersionNumber)
                .Select(sv => new SyllabusVersionDetailsDto
                {
                    Id = sv.Id,
                    VersionNumber = sv.VersionNumber,
                    EffectiveDate = sv.EffectiveDate,
                    IsActive = sv.IsActive,
                    CreatedBy = sv.CreatedBy,
                    CreatedAt = sv.CreatedAt,
                    HasContent = !string.IsNullOrWhiteSpace(sv.Content)
                })
                .ToList();

            subjectDto.Analysis = AnalyzeSubject(subjectDto);
            versionDto.Subjects.Add(subjectDto);
        }

        _logger.LogInformation(
            "Built details for {SubjectCount} subjects; total syllabus versions {SyllabusCount} (versionId {VersionId})",
            versionDto.Subjects.Count,
            versionDto.Subjects.Sum(s => s.SyllabusVersions.Count),
            version.Id);

        versionDto.Analysis = AnalyzeCurriculumVersion(versionDto);
        _logger.LogInformation(
            "Curriculum version analysis: TotalSubjects={TotalSubjects}, Mandatory={Mandatory}, Elective={Elective}, WithSyllabus={WithSyllabus}, Completion%={CompletionPercent}",
            versionDto.Analysis.TotalSubjects,
            versionDto.Analysis.MandatorySubjects,
            versionDto.Analysis.ElectiveSubjects,
            versionDto.Analysis.SubjectsWithSyllabus,
            versionDto.Analysis.SyllabusCompletionPercentage);

        return versionDto;
    }

    private SubjectAnalysisDto AnalyzeSubject(CurriculumSubjectDetailsDto subject)
    {
        var analysis = new SubjectAnalysisDto
        {
            TotalSyllabusVersions = subject.SyllabusVersions.Count,
            ActiveSyllabusVersions = subject.SyllabusVersions.Count(sv => sv.IsActive),
            HasAnySyllabus = subject.SyllabusVersions.Any(),
            HasActiveSyllabus = subject.SyllabusVersions.Any(sv => sv.IsActive)
        };
        var latestVersion = subject.SyllabusVersions.FirstOrDefault();
        analysis.HasContentInLatestVersion = latestVersion?.HasContent ?? false;
        analysis.Status = !analysis.HasAnySyllabus ? "Missing" : (!analysis.HasContentInLatestVersion ? "Incomplete" : "Complete");
        return analysis;
    }

    private CurriculumVersionAnalysisDto AnalyzeCurriculumVersion(CurriculumVersionDetailsDto version)
    {
        var analysis = new CurriculumVersionAnalysisDto
        {
            TotalSubjects = version.Subjects.Count,
            MandatorySubjects = version.Subjects.Count(s => s.IsMandatory),
            ElectiveSubjects = version.Subjects.Count(s => !s.IsMandatory),
            SubjectsWithSyllabus = version.Subjects.Count(s => s.Analysis.HasAnySyllabus),
            TotalSyllabusVersions = version.Subjects.Sum(s => s.SyllabusVersions.Count)
        };
        analysis.SubjectsWithoutSyllabus = analysis.TotalSubjects - analysis.SubjectsWithSyllabus;
        analysis.SyllabusCompletionPercentage = analysis.TotalSubjects > 0 ? Math.Round((double)analysis.SubjectsWithSyllabus / analysis.TotalSubjects * 100, 2) : 0;
        analysis.MissingContentSubjects = version.Subjects
            .Where(s => s.Analysis.Status != "Complete")
            .Select(s => $"{s.SubjectCode} - {s.SubjectName} ({s.Analysis.Status})")
            .ToList();
        return analysis;
    }
}