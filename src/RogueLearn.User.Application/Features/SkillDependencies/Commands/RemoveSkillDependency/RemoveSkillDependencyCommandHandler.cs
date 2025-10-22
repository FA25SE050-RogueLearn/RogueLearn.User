using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.SkillDependencies.Commands.RemoveSkillDependency;

/// <summary>
/// Handles removal of a Skill dependency relation.
/// - Loads dependency and throws standardized NotFoundException when missing.
/// - Emits structured logs for start and completion.
/// </summary>
public sealed class RemoveSkillDependencyCommandHandler : IRequestHandler<RemoveSkillDependencyCommand>
{
    private readonly ISkillDependencyRepository _repository;
    private readonly ILogger<RemoveSkillDependencyCommandHandler> _logger;

    public RemoveSkillDependencyCommandHandler(ISkillDependencyRepository repository, ILogger<RemoveSkillDependencyCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Removes the dependency between SkillId and PrerequisiteSkillId.
    /// </summary>
    public async Task Handle(RemoveSkillDependencyCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling RemoveSkillDependencyCommand for SkillId={SkillId}, PrereqSkillId={PrereqSkillId}", request.SkillId, request.PrerequisiteSkillId);

        var dep = await _repository.FirstOrDefaultAsync(d => d.SkillId == request.SkillId && d.PrerequisiteSkillId == request.PrerequisiteSkillId, cancellationToken);
        if (dep is null)
        {
            _logger.LogWarning("Skill dependency not found: SkillId={SkillId}, PrereqSkillId={PrereqSkillId}", request.SkillId, request.PrerequisiteSkillId);
            throw new NotFoundException("SkillDependency", $"{request.SkillId}:{request.PrerequisiteSkillId}");
        }

        await _repository.DeleteAsync(dep.Id, cancellationToken);
        _logger.LogInformation("Removed skill dependency: DepId={DepId}, SkillId={SkillId}, PrereqSkillId={PrereqSkillId}", dep.Id, request.SkillId, request.PrerequisiteSkillId);
    }
}