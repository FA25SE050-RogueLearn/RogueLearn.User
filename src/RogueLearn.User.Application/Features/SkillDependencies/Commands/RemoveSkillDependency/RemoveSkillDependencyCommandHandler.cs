using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.SkillDependencies.Commands.RemoveSkillDependency;

public sealed class RemoveSkillDependencyCommandHandler : IRequestHandler<RemoveSkillDependencyCommand>
{
    private readonly ISkillDependencyRepository _repository;

    public RemoveSkillDependencyCommandHandler(ISkillDependencyRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(RemoveSkillDependencyCommand request, CancellationToken cancellationToken)
    {
        var dep = await _repository.FirstOrDefaultAsync(d => d.SkillId == request.SkillId && d.PrerequisiteSkillId == request.PrerequisiteSkillId, cancellationToken);
        if (dep is null)
        {
            throw new KeyNotFoundException("Skill dependency not found");
        }

        await _repository.DeleteAsync(dep.Id, cancellationToken);
    }
}