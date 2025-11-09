// RogueLearn.User/src/RogueLearn.User.Application/Features/CurriculumProgramSubjects/Commands/RemoveSubjectFromProgram/RemoveSubjectFromProgramCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumProgramSubjects.Commands.RemoveSubjectFromProgram;

public class RemoveSubjectFromProgramCommandHandler : IRequestHandler<RemoveSubjectFromProgramCommand>
{
    private readonly ICurriculumProgramSubjectRepository _programSubjectRepository;
    private readonly ILogger<RemoveSubjectFromProgramCommandHandler> _logger;

    public RemoveSubjectFromProgramCommandHandler(
        ICurriculumProgramSubjectRepository programSubjectRepository,
        ILogger<RemoveSubjectFromProgramCommandHandler> logger)
    {
        _programSubjectRepository = programSubjectRepository;
        _logger = logger;
    }

    public async Task Handle(RemoveSubjectFromProgramCommand request, CancellationToken cancellationToken)
    {
        var mapping = await _programSubjectRepository.FirstOrDefaultAsync(
            ps => ps.ProgramId == request.ProgramId && ps.SubjectId == request.SubjectId,
            cancellationToken);

        if (mapping == null)
        {
            // For idempotency, if the link doesn't exist, we consider the operation successful.
            _logger.LogWarning("Attempted to remove a non-existent association between Program {ProgramId} and Subject {SubjectId}", request.ProgramId, request.SubjectId);
            return;
        }

        await _programSubjectRepository.DeleteAsync(mapping.Id, cancellationToken);

        _logger.LogInformation("Successfully removed association between Program {ProgramId} and Subject {SubjectId}", request.ProgramId, request.SubjectId);
    }
}