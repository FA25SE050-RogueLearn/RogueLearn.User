using AutoMapper;
using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumStructure.Queries.GetCurriculumStructureByVersion;

public class GetCurriculumStructureByVersionHandler : IRequestHandler<GetCurriculumStructureByVersionQuery, List<CurriculumStructureDto>>
{
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;

    public GetCurriculumStructureByVersionHandler(
        ICurriculumStructureRepository curriculumStructureRepository,
        ISubjectRepository subjectRepository,
        IMapper mapper)
    {
        _curriculumStructureRepository = curriculumStructureRepository;
        _subjectRepository = subjectRepository;
        _mapper = mapper;
    }

    public async Task<List<CurriculumStructureDto>> Handle(GetCurriculumStructureByVersionQuery request, CancellationToken cancellationToken)
    {
        var structures = await _curriculumStructureRepository.GetAllAsync(cancellationToken);
        var versionStructures = structures.Where(s => s.CurriculumVersionId == request.CurriculumVersionId).ToList();

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
                TermNumber = structure.TermNumber,
                IsMandatory = structure.IsMandatory,
                PrerequisiteSubjectIds = structure.PrerequisiteSubjectIds,
                PrerequisitesText = structure.PrerequisitesText,
                CreatedAt = structure.CreatedAt
            };

            result.Add(dto);
        }

        return result.OrderBy(r => r.TermNumber).ThenBy(r => r.SubjectCode).ToList();
    }
}