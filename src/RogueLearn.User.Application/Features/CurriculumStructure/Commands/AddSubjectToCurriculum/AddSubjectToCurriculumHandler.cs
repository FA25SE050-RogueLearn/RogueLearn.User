using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.CurriculumStructure.Commands.AddSubjectToCurriculum;

/// <summary>
/// Handles adding a Subject to a CurriculumVersion structure.
/// - Validates CurriculumVersion and Subject exist.
/// - Prevents duplicates and throws ConflictException when already present.
/// - Emits structured logs.
/// </summary>
public class AddSubjectToCurriculumHandler : IRequestHandler<AddSubjectToCurriculumCommand, AddSubjectToCurriculumResponse>
{
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly ICurriculumVersionRepository _curriculumVersionRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AddSubjectToCurriculumHandler> _logger;

    public AddSubjectToCurriculumHandler(
        ICurriculumStructureRepository curriculumStructureRepository,
        ICurriculumVersionRepository curriculumVersionRepository,
        ISubjectRepository subjectRepository,
        IMapper mapper,
        ILogger<AddSubjectToCurriculumHandler> logger)
    {
        _curriculumStructureRepository = curriculumStructureRepository;
        _curriculumVersionRepository = curriculumVersionRepository;
        _subjectRepository = subjectRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Adds the subject to the curriculum structure after validation and duplicate prevention.
    /// </summary>
    public async Task<AddSubjectToCurriculumResponse> Handle(AddSubjectToCurriculumCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling AddSubjectToCurriculumCommand for CurriculumVersionId={CurriculumVersionId}, SubjectId={SubjectId}", request.CurriculumVersionId, request.SubjectId);

        // Validate curriculum version exists
        var curriculumVersion = await _curriculumVersionRepository.GetByIdAsync(request.CurriculumVersionId, cancellationToken);
        if (curriculumVersion == null)
        {
            _logger.LogWarning("CurriculumVersion not found: CurriculumVersionId={CurriculumVersionId}", request.CurriculumVersionId);
            throw new NotFoundException("CurriculumVersion", request.CurriculumVersionId);
        }

        // Validate subject exists
        var subject = await _subjectRepository.GetByIdAsync(request.SubjectId, cancellationToken);
        if (subject == null)
        {
            _logger.LogWarning("Subject not found: SubjectId={SubjectId}", request.SubjectId);
            throw new NotFoundException("Subject", request.SubjectId);
        }

        // Check if subject is already in the curriculum structure using a targeted query
        var existingStructure = await _curriculumStructureRepository.FirstOrDefaultAsync(
            s => s.CurriculumVersionId == request.CurriculumVersionId && s.SubjectId == request.SubjectId,
            cancellationToken);

        if (existingStructure != null)
        {
            _logger.LogInformation("Add prevented: SubjectId={SubjectId} already part of CurriculumVersionId={CurriculumVersionId}", request.SubjectId, request.CurriculumVersionId);
            throw new ConflictException($"Subject {subject.SubjectCode} is already part of this curriculum version.");
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
        _logger.LogInformation("Added subject to curriculum structure: CurriculumVersionId={CurriculumVersionId}, SubjectId={SubjectId}, CurriculumStructureId={CurriculumStructureId}", request.CurriculumVersionId, request.SubjectId, created.Id);
        return _mapper.Map<AddSubjectToCurriculumResponse>(created);
    }
}