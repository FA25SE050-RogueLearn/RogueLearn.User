// RogueLearn.User/src/RogueLearn.User.Application/Features/SubjectSkillMappings/Commands/RemoveSubjectSkillMapping/RemoveSubjectSkillMappingCommandHandler.cs
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.SubjectSkillMappings.Commands.RemoveSubjectSkillMapping;

public class RemoveSubjectSkillMappingCommandHandler : IRequestHandler<RemoveSubjectSkillMappingCommand>
{
    private readonly ISubjectSkillMappingRepository _repository;

    public RemoveSubjectSkillMappingCommandHandler(ISubjectSkillMappingRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(RemoveSubjectSkillMappingCommand request, CancellationToken cancellationToken)
    {
        var mapping = await _repository.FirstOrDefaultAsync(
            m => m.SubjectId == request.SubjectId && m.SkillId == request.SkillId,
            cancellationToken);

        if (mapping == null)
        {
            throw new NotFoundException("Subject-skill mapping not found.");
        }

        await _repository.DeleteAsync(mapping.Id, cancellationToken);
    }
}