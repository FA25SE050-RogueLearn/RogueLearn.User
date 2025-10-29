// RogueLearn.User/src/RogueLearn.User.Application/Features/CurriculumVersions/Queries/GetCurriculumForQuestGeneration/GetCurriculumForQuestGenerationQueryHandler.cs
using MediatR;
using RogueLearn.User.Application.DTOs.Internal;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.CurriculumVersions.Queries.GetCurriculumForQuestGeneration;

public class GetCurriculumForQuestGenerationQueryHandler : IRequestHandler<GetCurriculumForQuestGenerationQuery, CurriculumForQuestGenerationDto>
{
    private readonly ICurriculumVersionRepository _versionRepository;
    private readonly ICurriculumProgramRepository _programRepository;
    private readonly ICurriculumStructureRepository _structureRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<GetCurriculumForQuestGenerationQueryHandler> _logger;

    public GetCurriculumForQuestGenerationQueryHandler(
        ICurriculumVersionRepository versionRepository,
        ICurriculumProgramRepository programRepository,
        ICurriculumStructureRepository structureRepository,
        ISubjectRepository subjectRepository,
        ILogger<GetCurriculumForQuestGenerationQueryHandler> logger)
    {
        _versionRepository = versionRepository;
        _programRepository = programRepository;
        _structureRepository = structureRepository;
        _subjectRepository = subjectRepository;
        _logger = logger;
    }

    /// <summary>
    /// Builds a CurriculumForQuestGenerationDto for the given curriculum version by joining program info,
    /// curriculum structures and subject details, ordered by semester.
    /// </summary>
    /// <param name="request">The request containing the CurriculumVersionId.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A CurriculumForQuestGenerationDto with program and ordered subject details.</returns>
    public async Task<CurriculumForQuestGenerationDto> Handle(GetCurriculumForQuestGenerationQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing quest generation curriculum for versionId {VersionId}", request.CurriculumVersionId);

        var version = await _versionRepository.GetByIdAsync(request.CurriculumVersionId, cancellationToken);
        if (version == null)
        {
            _logger.LogWarning("CurriculumVersion not found for id {VersionId}", request.CurriculumVersionId);
            throw new NotFoundException("CurriculumVersion", request.CurriculumVersionId);
        }

        var program = await _programRepository.GetByIdAsync(version.ProgramId, cancellationToken);
        if (program == null)
        {
            _logger.LogWarning("CurriculumProgram not found for id {ProgramId} (versionId {VersionId})", version.ProgramId, version.Id);
            throw new NotFoundException("CurriculumProgram", version.ProgramId);
        }

        var structures = await _structureRepository.FindAsync(s => s.CurriculumVersionId == version.Id, cancellationToken) ?? Enumerable.Empty<Domain.Entities.CurriculumStructure>();
        var structureCount = structures.Count();
        _logger.LogInformation("Found {StructureCount} curriculum structures for version {VersionId}", structureCount, version.Id);

        var subjectIds = structures.Select(s => s.SubjectId).ToList();
        var subjects = (await _subjectRepository.GetAllAsync(cancellationToken)).Where(s => subjectIds.Contains(s.Id));
        _logger.LogInformation("Matched {SubjectCount} unique subjects for version {VersionId}", subjects.Count(), version.Id);

        var subjectDtos = new List<SubjectForQuestGenerationDto>();
        foreach (var structure in structures)
        {
            var subject = subjects.FirstOrDefault(s => s.Id == structure.SubjectId);
            if (subject != null)
            {
                subjectDtos.Add(new SubjectForQuestGenerationDto
                {
                    Code = subject.SubjectCode,
                    Name = subject.SubjectName,
                    Semester = structure.Semester,
                    Credits = subject.Credits,
                    Prerequisites = structure.PrerequisitesText
                });
            }
            else
            {
                _logger.LogWarning("Subject id {SubjectId} referenced by curriculum structure not found (versionId {VersionId})", structure.SubjectId, version.Id);
            }
        }

        var orderedSubjects = subjectDtos.OrderBy(s => s.Semester).ToList();
        _logger.LogInformation("Returning {SubjectCount} subjects for program {ProgramCode}", orderedSubjects.Count, program.ProgramCode);

        return new CurriculumForQuestGenerationDto
        {
            CurriculumCode = program.ProgramCode,
            Name = program.ProgramName,
            Subjects = orderedSubjects
        };
    }
}