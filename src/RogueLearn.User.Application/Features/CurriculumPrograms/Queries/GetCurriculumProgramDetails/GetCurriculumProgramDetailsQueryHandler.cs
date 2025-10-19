// RogueLearn.User/src/RogueLearn.User.Application/Features/CurriculumPrograms/Queries/GetCurriculumProgramDetails/GetCurriculumProgramDetailsQueryHandler.cs
using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Entities; // ADD THIS USING

namespace RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;

public class GetCurriculumProgramDetailsQueryHandler : IRequestHandler<GetCurriculumProgramDetailsQuery, CurriculumProgramDetailsResponse>
{
    private readonly ICurriculumProgramRepository _curriculumProgramRepository;
    private readonly ICurriculumVersionRepository _curriculumVersionRepository;
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ISyllabusVersionRepository _syllabusVersionRepository;
    private readonly IMapper _mapper;

    public GetCurriculumProgramDetailsQueryHandler(
        ICurriculumProgramRepository curriculumProgramRepository,
        ICurriculumVersionRepository curriculumVersionRepository,
        ICurriculumStructureRepository curriculumStructureRepository,
        ISubjectRepository subjectRepository,
        ISyllabusVersionRepository syllabusVersionRepository,
        IMapper mapper)
    {
        _curriculumProgramRepository = curriculumProgramRepository;
        _curriculumVersionRepository = curriculumVersionRepository;
        _curriculumStructureRepository = curriculumStructureRepository;
        _subjectRepository = subjectRepository;
        _syllabusVersionRepository = syllabusVersionRepository;
        _mapper = mapper;
    }

    public async Task<CurriculumProgramDetailsResponse> Handle(GetCurriculumProgramDetailsQuery request, CancellationToken cancellationToken)
    {
        CurriculumProgram? program;

        // MODIFIED LOGIC: Determine how to fetch the program based on the provided ID.
        if (request.VersionId.HasValue)
        {
            // If a VersionId is provided, find the version first.
            var version = await _curriculumVersionRepository.GetByIdAsync(request.VersionId.Value, cancellationToken);
            if (version == null)
            {
                // This is the error the QuestService will now receive if the ID is wrong.
                throw new NotFoundException("CurriculumVersion", request.VersionId.Value);
            }
            // Then, use the ProgramId from the version to get the program.
            program = await _curriculumProgramRepository.GetByIdAsync(version.ProgramId, cancellationToken);
        }
        else if (request.ProgramId.HasValue)
        {
            // If a ProgramId is provided, fetch the program directly.
            program = await _curriculumProgramRepository.GetByIdAsync(request.ProgramId.Value, cancellationToken);
        }
        else
        {
            throw new BadRequestException("Either a ProgramId or a VersionId must be provided.");
        }

        // The original error occurred here because a VersionId was used to search the program repository.
        if (program == null)
        {
            throw new NotFoundException("CurriculumProgram", request.ProgramId ?? request.VersionId ?? Guid.Empty);
        }

        var response = _mapper.Map<CurriculumProgramDetailsResponse>(program);

        // Get all curriculum versions for this program
        var curriculumVersions = await _curriculumVersionRepository.FindAsync(
            cv => cv.ProgramId == program.Id,
            cancellationToken);

        response.CurriculumVersions = new List<CurriculumVersionDetailsDto>();

        foreach (var version in curriculumVersions.OrderByDescending(v => v.EffectiveYear))
        {
            var versionDto = _mapper.Map<CurriculumVersionDetailsDto>(version);

            // Get curriculum structure (subjects) for this version
            var curriculumStructures = await _curriculumStructureRepository.FindAsync(
                cs => cs.CurriculumVersionId == version.Id,
                cancellationToken);

            versionDto.Subjects = new List<CurriculumSubjectDetailsDto>();

            foreach (var structure in curriculumStructures.OrderBy(s => s.TermNumber).ThenBy(s => s.IsMandatory ? 0 : 1))
            {
                // Get subject details
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

                // Get syllabus versions for this subject
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

                // Analyze subject syllabus status
                subjectDto.Analysis = AnalyzeSubject(subjectDto);

                versionDto.Subjects.Add(subjectDto);
            }

            // Analyze curriculum version
            versionDto.Analysis = AnalyzeCurriculumVersion(versionDto);

            response.CurriculumVersions.Add(versionDto);
        }

        // Analyze overall curriculum program
        response.Analysis = AnalyzeCurriculumProgram(response);

        return response;
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

        // Determine status
        if (!analysis.HasAnySyllabus)
        {
            analysis.Status = "Missing";
        }
        else if (!analysis.HasContentInLatestVersion)
        {
            analysis.Status = "Incomplete";
        }
        else
        {
            analysis.Status = "Complete";
        }

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
        analysis.SyllabusCompletionPercentage = analysis.TotalSubjects > 0
            ? Math.Round((double)analysis.SubjectsWithSyllabus / analysis.TotalSubjects * 100, 2)
            : 0;

        analysis.MissingContentSubjects = version.Subjects
            .Where(s => s.Analysis.Status != "Complete")
            .Select(s => $"{s.SubjectCode} - {s.SubjectName} ({s.Analysis.Status})")
            .ToList();

        return analysis;
    }

    private CurriculumAnalysisDto AnalyzeCurriculumProgram(CurriculumProgramDetailsResponse program)
    {
        var allSubjects = program.CurriculumVersions.SelectMany(v => v.Subjects).ToList();
        var uniqueSubjects = allSubjects.GroupBy(s => s.SubjectId).Select(g => g.First()).ToList();

        var analysis = new CurriculumAnalysisDto
        {
            TotalVersions = program.CurriculumVersions.Count,
            ActiveVersions = program.CurriculumVersions.Count(v => v.IsActive),
            TotalSubjects = uniqueSubjects.Count,
            SubjectsWithSyllabus = uniqueSubjects.Count(s => s.Analysis.HasAnySyllabus),
            TotalSyllabusVersions = allSubjects.Sum(s => s.SyllabusVersions.Count)
        };

        analysis.SubjectsWithoutSyllabus = analysis.TotalSubjects - analysis.SubjectsWithSyllabus;
        analysis.SyllabusCompletionPercentage = analysis.TotalSubjects > 0
            ? Math.Round((double)analysis.SubjectsWithSyllabus / analysis.TotalSubjects * 100, 2)
            : 0;

        analysis.MissingContentSubjects = uniqueSubjects
            .Where(s => s.Analysis.Status != "Complete")
            .Select(s => $"{s.SubjectCode} - {s.SubjectName} ({s.Analysis.Status})")
            .Distinct()
            .ToList();

        return analysis;
    }
}