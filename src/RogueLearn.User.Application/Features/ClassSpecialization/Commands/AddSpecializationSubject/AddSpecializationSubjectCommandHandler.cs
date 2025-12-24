using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.ClassSpecialization.Queries.GetSpecializationSubjects;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.ClassSpecialization.Commands.AddSpecializationSubject;

/// <summary>
/// Handler for adding a specialization subject mapping to a class.
/// - Validates existence of Class and Subject.
/// - Enforces idempotency by preventing duplicate mappings.
/// - Uses AutoMapper without overwriting domain-generated identifiers.
/// - Emits structured logs for traceability.
/// </summary>
public class AddSpecializationSubjectCommandHandler : IRequestHandler<AddSpecializationSubjectCommand, SpecializationSubjectDto>
{
    private readonly IClassSpecializationSubjectRepository _repository;
    private readonly IClassRepository _classRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AddSpecializationSubjectCommandHandler> _logger;

    public AddSpecializationSubjectCommandHandler(
        IClassSpecializationSubjectRepository repository,
        IClassRepository classRepository,
        ISubjectRepository subjectRepository,
        IMapper mapper,
        ILogger<AddSpecializationSubjectCommandHandler> logger)
    {
        _repository = repository;
        _classRepository = classRepository;
        _subjectRepository = subjectRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="AddSpecializationSubjectCommand"/> request by validating inputs, enforcing idempotency, and creating the mapping.
    /// </summary>
    /// <param name="request">The command payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="SpecializationSubjectDto"/> representing the created mapping.</returns>
    /// <exception cref="NotFoundException">Thrown when Class or Subject does not exist.</exception>
    /// <exception cref="BadRequestException">Thrown when the mapping already exists.</exception>
    public async Task<SpecializationSubjectDto> Handle(AddSpecializationSubjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling AddSpecializationSubjectCommand for ClassId={ClassId}, SubjectId={SubjectId}",
            request.ClassId, request.SubjectId);

        // Validate that both the class and subject exist
        if (!await _classRepository.ExistsAsync(request.ClassId, cancellationToken))
        {
            _logger.LogWarning("Class not found: {ClassId}", request.ClassId);
            throw new NotFoundException(nameof(Class), request.ClassId);
        }

        // Fetch subject to ensure it exists
        var subject = await _subjectRepository.GetByIdAsync(request.SubjectId, cancellationToken);
        if (subject == null)
        {
            _logger.LogWarning("Subject not found: {SubjectId}", request.SubjectId);
            throw new NotFoundException(nameof(Subject), request.SubjectId);
        }

        // Prevent duplicate mappings
        var existing = await _repository.FirstOrDefaultAsync(m => m.ClassId == request.ClassId && m.SubjectId == request.SubjectId, cancellationToken);
        if (existing != null)
        {
            _logger.LogInformation("Duplicate specialization subject mapping detected for ClassId={ClassId}, SubjectId={SubjectId}", request.ClassId, request.SubjectId);
            throw new BadRequestException("This subject is already mapped to this specialization class.");
        }

        // Map command to entity
        var newMapping = new ClassSpecializationSubject
        {
            ClassId = request.ClassId,
            SubjectId = request.SubjectId
        };

        var createdMapping = await _repository.AddAsync(newMapping, cancellationToken);

        _logger.LogInformation("Created specialization subject mapping: MappingId={MappingId} for ClassId={ClassId}, SubjectId={SubjectId}", createdMapping.Id, request.ClassId, request.SubjectId);

        return _mapper.Map<SpecializationSubjectDto>(createdMapping);
    }
}