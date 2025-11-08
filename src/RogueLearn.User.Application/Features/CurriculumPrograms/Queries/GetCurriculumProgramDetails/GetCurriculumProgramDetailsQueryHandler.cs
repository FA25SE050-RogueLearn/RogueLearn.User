// RogueLearn.User/src/RogueLearn.User.Application/Features/CurriculumPrograms/Queries/GetCurriculumProgramDetails/GetCurriculumProgramDetailsQueryHandler.cs
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;

public class GetCurriculumProgramDetailsQueryHandler : IRequestHandler<GetCurriculumProgramDetailsQuery, CurriculumProgramDetailsResponse>
{
    private readonly ICurriculumProgramRepository _curriculumProgramRepository;
    private readonly ICurriculumProgramSubjectRepository _programSubjectRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCurriculumProgramDetailsQueryHandler> _logger;

    public GetCurriculumProgramDetailsQueryHandler(
        ICurriculumProgramRepository curriculumProgramRepository,
        ICurriculumProgramSubjectRepository programSubjectRepository,
        ISubjectRepository subjectRepository,
        IMapper mapper,
        ILogger<GetCurriculumProgramDetailsQueryHandler> logger)
    {
        _curriculumProgramRepository = curriculumProgramRepository;
        _programSubjectRepository = programSubjectRepository;
        _subjectRepository = subjectRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<CurriculumProgramDetailsResponse> Handle(GetCurriculumProgramDetailsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetCurriculumProgramDetailsQuery for ProgramId={ProgramId}", request.ProgramId);

        if (!request.ProgramId.HasValue)
        {
            throw new BadRequestException("A ProgramId must be provided.");
        }

        var program = await _curriculumProgramRepository.GetByIdAsync(request.ProgramId.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(CurriculumProgram), request.ProgramId.Value);

        var response = _mapper.Map<CurriculumProgramDetailsResponse>(program);

        // Get all subject mappings for this program
        var programSubjectLinks = (await _programSubjectRepository.FindAsync(ps => ps.ProgramId == program.Id, cancellationToken)).ToList();
        var subjectIds = programSubjectLinks.Select(ps => ps.SubjectId).ToList();

        if (!subjectIds.Any())
        {
            _logger.LogInformation("No subjects found for program {ProgramId}", program.Id);
            response.Analysis = AnalyzeCurriculumProgram(response); // Run analysis on empty set
            return response;
        }

        // Get all subject details for the linked subjects
        var allSubjects = (await _subjectRepository.GetAllAsync(cancellationToken))
            .Where(s => subjectIds.Contains(s.Id))
            .ToList();

        // Group subjects by version to create the "version" DTOs
        var subjectsByVersion = allSubjects.GroupBy(s => s.Version).OrderByDescending(g => g.Key);

        foreach (var versionGroup in subjectsByVersion)
        {
            var versionDto = new CurriculumVersionDetailsDto
            {
                // In the new model, the "version" is just a string identifier (e.g., "2022")
                // We derive a synthetic ID and other fields for DTO compatibility.
                Id = Guid.NewGuid(), // Synthetic ID
                VersionCode = versionGroup.Key,
                EffectiveYear = int.TryParse(versionGroup.Key, out var year) ? year : 0,
                IsActive = true, // Assume all found versions are active for this view
                CreatedAt = versionGroup.Min(s => s.CreatedAt)
            };

            foreach (var subject in versionGroup.OrderBy(s => s.Semester).ThenBy(s => s.SubjectCode))
            {
                var subjectDto = new CurriculumSubjectDetailsDto
                {
                    SubjectId = subject.Id,
                    SubjectCode = subject.SubjectCode,
                    SubjectName = subject.SubjectName,
                    Credits = subject.Credits,
                    Description = subject.Description,
                    TermNumber = subject.Semester,
                    PrerequisiteSubjectIds = subject.PrerequisiteSubjectIds,
                    // IsMandatory can be a future extension on the join table, default to true for now
                    IsMandatory = true,
                    Analysis = AnalyzeSubject(subject)
                };
                versionDto.Subjects.Add(subjectDto);
            }

            versionDto.Analysis = AnalyzeCurriculumVersion(versionDto);
            response.CurriculumVersions.Add(versionDto);
        }

        response.Analysis = AnalyzeCurriculumProgram(response);
        _logger.LogInformation("Returning details for Program {ProgramId}: {VersionsCount} versions, {UniqueSubjectsCount} unique subjects.",
            program.Id, response.CurriculumVersions.Count, allSubjects.Count);

        return response;
    }

    // Analysis logic now works with the simplified entities
    private SubjectAnalysisDto AnalyzeSubject(Subject subject)
    {
        var hasContent = subject.Content != null && subject.Content.Any();
        return new SubjectAnalysisDto
        {
            HasContentInLatestVersion = hasContent,
            Status = hasContent ? "Complete" : "Missing"
        };
    }

    private CurriculumVersionAnalysisDto AnalyzeCurriculumVersion(CurriculumVersionDetailsDto version)
    {
        var analysis = new CurriculumVersionAnalysisDto
        {
            TotalSubjects = version.Subjects.Count,
            MandatorySubjects = version.Subjects.Count(s => s.IsMandatory),
            ElectiveSubjects = version.Subjects.Count(s => !s.IsMandatory),
            SubjectsWithContent = version.Subjects.Count(s => s.Analysis.HasContentInLatestVersion)
        };
        analysis.SubjectsWithoutContent = analysis.TotalSubjects - analysis.SubjectsWithContent;
        analysis.ContentCompletionPercentage = analysis.TotalSubjects > 0
            ? Math.Round((double)analysis.SubjectsWithContent / analysis.TotalSubjects * 100, 2)
            : 0;
        analysis.MissingContentSubjects = version.Subjects
            .Where(s => s.Analysis.Status != "Complete")
            .Select(s => $"{s.SubjectCode} ({s.Analysis.Status})")
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
            TotalSubjects = uniqueSubjects.Count,
            SubjectsWithContent = uniqueSubjects.Count(s => s.Analysis.HasContentInLatestVersion)
        };
        analysis.SubjectsWithoutContent = analysis.TotalSubjects - analysis.SubjectsWithContent;
        analysis.ContentCompletionPercentage = analysis.TotalSubjects > 0
            ? Math.Round((double)analysis.SubjectsWithContent / analysis.TotalSubjects * 100, 2)
            : 0;
        analysis.MissingContentSubjects = uniqueSubjects
            .Where(s => s.Analysis.Status != "Complete")
            .Select(s => $"{s.SubjectCode} ({s.Analysis.Status})")
            .Distinct()
            .ToList();
        return analysis;
    }
}