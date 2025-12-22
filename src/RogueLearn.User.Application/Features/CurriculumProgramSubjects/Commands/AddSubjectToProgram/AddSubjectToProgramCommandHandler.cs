using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumProgramSubjects.Commands.AddSubjectToProgram;

public class AddSubjectToProgramCommandHandler : IRequestHandler<AddSubjectToProgramCommand, AddSubjectToProgramResponse>
{
    private readonly ICurriculumProgramRepository _programRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ICurriculumProgramSubjectRepository _programSubjectRepository;
    private readonly ILogger<AddSubjectToProgramCommandHandler> _logger;

    public AddSubjectToProgramCommandHandler(
        ICurriculumProgramRepository programRepository,
        ISubjectRepository subjectRepository,
        ICurriculumProgramSubjectRepository programSubjectRepository,
        ILogger<AddSubjectToProgramCommandHandler> logger)
    {
        _programRepository = programRepository;
        _subjectRepository = subjectRepository;
        _programSubjectRepository = programSubjectRepository;
        _logger = logger;
    }

    public async Task<AddSubjectToProgramResponse> Handle(AddSubjectToProgramCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate that the program and subject both exist.
        if (!await _programRepository.ExistsAsync(request.ProgramId, cancellationToken))
        {
            throw new NotFoundException(nameof(CurriculumProgram), request.ProgramId);
        }
        if (!await _subjectRepository.ExistsAsync(request.SubjectId, cancellationToken))
        {
            throw new NotFoundException(nameof(Subject), request.SubjectId);
        }

        // 2. Check for an existing mapping to ensure idempotency.
        var mappingExists = await _programSubjectRepository.AnyAsync(
            ps => ps.ProgramId == request.ProgramId && ps.SubjectId == request.SubjectId,
            cancellationToken);

        if (mappingExists)
        {
            throw new ConflictException("The subject is already associated with this curriculum program.");
        }

        // 3. Create and persist the new association.
        var newMapping = new CurriculumProgramSubject
        {
            ProgramId = request.ProgramId,
            SubjectId = request.SubjectId
        };

        var createdMapping = await _programSubjectRepository.AddAsync(newMapping, cancellationToken);

        _logger.LogInformation("Successfully associated Subject {SubjectId} with Program {ProgramId}", request.SubjectId, request.ProgramId);

        return new AddSubjectToProgramResponse
        {
            ProgramId = createdMapping.ProgramId,
            SubjectId = createdMapping.SubjectId,
            CreatedAt = createdMapping.CreatedAt
        };
    }
}