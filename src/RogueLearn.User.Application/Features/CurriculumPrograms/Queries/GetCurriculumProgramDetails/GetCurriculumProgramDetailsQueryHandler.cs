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

        var programSubjectLinks = (await _programSubjectRepository.FindAsync(ps => ps.ProgramId == program.Id, cancellationToken)).ToList();
        var subjectIds = programSubjectLinks.Select(ps => ps.SubjectId).ToList();

        if (!subjectIds.Any())
        {
            _logger.LogInformation("No subjects found for program {ProgramId}", program.Id);
            response.Analysis = AnalyzeCurriculumProgram(response);
            return response;
        }

        var allSubjectsInProgram = (await _subjectRepository.GetAllAsync(cancellationToken))
            .Where(s => subjectIds.Contains(s.Id))
            .ToList();

        var singleVersionDto = new CurriculumVersionDetailsDto
        {
            Id = Guid.NewGuid(), // Synthetic ID for DTO compatibility
            VersionCode = "Current",
            IsActive = true,
            CreatedAt = allSubjectsInProgram.Any() ? allSubjectsInProgram.Min(s => s.CreatedAt) : program.CreatedAt
        };

        foreach (var subject in allSubjectsInProgram.OrderBy(s => s.Semester).ThenBy(s => s.SubjectCode))
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
                IsMandatory = true, // Defaulting as before
                Analysis = AnalyzeSubject(subject)
            };
            singleVersionDto.Subjects.Add(subjectDto);
        }

        singleVersionDto.Analysis = AnalyzeCurriculumVersion(singleVersionDto);
        response.CurriculumVersions.Add(singleVersionDto);

        response.Analysis = AnalyzeCurriculumProgram(response);
        _logger.LogInformation("Returning details for Program {ProgramId}: {SubjectsCount} subjects.",
            program.Id, allSubjectsInProgram.Count);

        return response;
    }

    private SubjectAnalysisDto AnalyzeSubject(Subject subject)
    {
        // MODIFIED: The check for content is updated to correctly handle a Dictionary, not a string.
        // It now checks if the dictionary is not null and has any elements.
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

        var analysis = new CurriculumAnalysisDto
        {
            TotalVersions = program.CurriculumVersions.Count,
            TotalSubjects = allSubjects.Count,
            SubjectsWithContent = allSubjects.Count(s => s.Analysis.HasContentInLatestVersion)
        };
        analysis.SubjectsWithoutContent = analysis.TotalSubjects - analysis.SubjectsWithContent;
        analysis.ContentCompletionPercentage = analysis.TotalSubjects > 0
            ? Math.Round((double)analysis.SubjectsWithContent / analysis.TotalSubjects * 100, 2)
            : 0;
        analysis.MissingContentSubjects = allSubjects
            .Where(s => s.Analysis.Status != "Complete")
            .Select(s => $"{s.SubjectCode} ({s.Analysis.Status})")
            .Distinct()
            .ToList();
        return analysis;
    }
}