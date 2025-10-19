// RogueLearn.User/src/RogueLearn.User.Application/Features/CurriculumVersions/Queries/GetCurriculumForQuestGeneration/GetCurriculumForQuestGenerationQueryHandler.cs
using MediatR;
using RogueLearn.User.Application.DTOs.Internal;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumVersions.Queries.GetCurriculumForQuestGeneration;

public class GetCurriculumForQuestGenerationQueryHandler : IRequestHandler<GetCurriculumForQuestGenerationQuery, CurriculumForQuestGenerationDto>
{
    private readonly ICurriculumVersionRepository _versionRepository;
    private readonly ICurriculumProgramRepository _programRepository;
    private readonly ICurriculumStructureRepository _structureRepository;
    private readonly ISubjectRepository _subjectRepository;

    public GetCurriculumForQuestGenerationQueryHandler(
        ICurriculumVersionRepository versionRepository,
        ICurriculumProgramRepository programRepository,
        ICurriculumStructureRepository structureRepository,
        ISubjectRepository subjectRepository)
    {
        _versionRepository = versionRepository;
        _programRepository = programRepository;
        _structureRepository = structureRepository;
        _subjectRepository = subjectRepository;
    }

    public async Task<CurriculumForQuestGenerationDto> Handle(GetCurriculumForQuestGenerationQuery request, CancellationToken cancellationToken)
    {
        var version = await _versionRepository.GetByIdAsync(request.CurriculumVersionId, cancellationToken);
        if (version == null)
        {
            throw new NotFoundException("CurriculumVersion", request.CurriculumVersionId);
        }

        var program = await _programRepository.GetByIdAsync(version.ProgramId, cancellationToken);
        if (program == null)
        {
            throw new NotFoundException("CurriculumProgram", version.ProgramId);
        }

        var structures = await _structureRepository.FindAsync(s => s.CurriculumVersionId == version.Id, cancellationToken);
        var subjectIds = structures.Select(s => s.SubjectId).ToList();
        var subjects = (await _subjectRepository.GetAllAsync(cancellationToken)).Where(s => subjectIds.Contains(s.Id));

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
                    Semester = structure.TermNumber,
                    Credits = subject.Credits,
                    Prerequisites = structure.PrerequisitesText
                });
            }
        }

        return new CurriculumForQuestGenerationDto
        {
            CurriculumCode = program.ProgramCode,
            Name = program.ProgramName,
            Subjects = subjectDtos.OrderBy(s => s.Semester).ToList()
        };
    }
}