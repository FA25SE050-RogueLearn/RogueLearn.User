// RogueLearn.User/src/RogueLearn.User.Application/Features/CurriculumVersions/Queries/GetCurriculumVersionDetails/GetCurriculumVersionDetailsQueryHandler.cs
using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumVersions.Queries.GetCurriculumVersionDetails;

public class GetCurriculumVersionDetailsQueryHandler : IRequestHandler<GetCurriculumVersionDetailsQuery, CurriculumVersionDetailsDto>
{
    private readonly ICurriculumVersionRepository _curriculumVersionRepository;
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ISyllabusVersionRepository _syllabusVersionRepository;
    private readonly IMapper _mapper;

    public GetCurriculumVersionDetailsQueryHandler(
        ICurriculumVersionRepository curriculumVersionRepository,
        ICurriculumStructureRepository curriculumStructureRepository,
        ISubjectRepository subjectRepository,
        ISyllabusVersionRepository syllabusVersionRepository,
        IMapper mapper)
    {
        _curriculumVersionRepository = curriculumVersionRepository;
        _curriculumStructureRepository = curriculumStructureRepository;
        _subjectRepository = subjectRepository;
        _syllabusVersionRepository = syllabusVersionRepository;
        _mapper = mapper;
    }

    public async Task<CurriculumVersionDetailsDto> Handle(GetCurriculumVersionDetailsQuery request, CancellationToken cancellationToken)
    {
        // Get the curriculum version directly
        var version = await _curriculumVersionRepository.GetByIdAsync(request.CurriculumVersionId, cancellationToken);
        if (version == null)
        {
            throw new NotFoundException("CurriculumVersion", request.CurriculumVersionId);
        }

        var versionDto = _mapper.Map<CurriculumVersionDetailsDto>(version);

        // Get curriculum structure (subjects) for this version
        var curriculumStructures = await _curriculumStructureRepository.FindAsync(
            cs => cs.CurriculumVersionId == version.Id,
            cancellationToken);

        versionDto.Subjects = new List<CurriculumSubjectDetailsDto>();

        foreach (var structure in curriculumStructures.OrderBy(s => s.TermNumber).ThenBy(s => s.IsMandatory ? 0 : 1))
        {
            var subject = await _subjectRepository.GetByIdAsync(structure.SubjectId, cancellationToken);
            if (subject == null) continue;

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
                cancellationToken);

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

        versionDto.Analysis = AnalyzeCurriculumVersion(versionDto);
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