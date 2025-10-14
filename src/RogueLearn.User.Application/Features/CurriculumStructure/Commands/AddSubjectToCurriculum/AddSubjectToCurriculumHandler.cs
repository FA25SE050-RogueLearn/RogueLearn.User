using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumStructure.Commands.AddSubjectToCurriculum;

public class AddSubjectToCurriculumHandler : IRequestHandler<AddSubjectToCurriculumCommand, AddSubjectToCurriculumResponse>
{
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly ICurriculumVersionRepository _curriculumVersionRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;

    public AddSubjectToCurriculumHandler(
        ICurriculumStructureRepository curriculumStructureRepository,
        ICurriculumVersionRepository curriculumVersionRepository,
        ISubjectRepository subjectRepository,
        IMapper mapper)
    {
        _curriculumStructureRepository = curriculumStructureRepository;
        _curriculumVersionRepository = curriculumVersionRepository;
        _subjectRepository = subjectRepository;
        _mapper = mapper;
    }

    public async Task<AddSubjectToCurriculumResponse> Handle(AddSubjectToCurriculumCommand request, CancellationToken cancellationToken)
    {
        // Validate curriculum version exists
        var curriculumVersion = await _curriculumVersionRepository.GetByIdAsync(request.CurriculumVersionId, cancellationToken);
        if (curriculumVersion == null)
        {
            throw new NotFoundException("CurriculumVersion", request.CurriculumVersionId);
        }

        // Validate subject exists
        var subject = await _subjectRepository.GetByIdAsync(request.SubjectId, cancellationToken);
        if (subject == null)
        {
            throw new NotFoundException("Subject", request.SubjectId);
        }

        // Check if subject is already in the curriculum structure
        var existingStructures = await _curriculumStructureRepository.GetAllAsync(cancellationToken);
        var existingStructure = existingStructures.FirstOrDefault(s => 
            s.CurriculumVersionId == request.CurriculumVersionId && 
            s.SubjectId == request.SubjectId);

        if (existingStructure != null)
        {
            throw new InvalidOperationException($"Subject {subject.SubjectCode} is already part of this curriculum version.");
        }

        var curriculumStructure = new Domain.Entities.CurriculumStructure
        {
            CurriculumVersionId = request.CurriculumVersionId,
            SubjectId = request.SubjectId,
            TermNumber = request.TermNumber,
            IsMandatory = request.IsMandatory,
            PrerequisiteSubjectIds = request.PrerequisiteSubjectIds,
            PrerequisitesText = request.PrerequisitesText
        };

        var created = await _curriculumStructureRepository.AddAsync(curriculumStructure, cancellationToken);
        return _mapper.Map<AddSubjectToCurriculumResponse>(created);
    }
}