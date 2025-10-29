using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.SkillDependencies.Commands.AddSkillDependency;

/// <summary>
/// Handles creation of a Skill dependency relation.
/// - Prevents duplicate dependencies and throws ConflictException.
/// - Emits structured logs for start and completion.
/// </summary>
public sealed class AddSkillDependencyCommandHandler : IRequestHandler<AddSkillDependencyCommand, AddSkillDependencyResponse>
{
    private readonly ISkillDependencyRepository _repository;
    private readonly ILogger<AddSkillDependencyCommandHandler> _logger;

    public AddSkillDependencyCommandHandler(ISkillDependencyRepository repository, ILogger<AddSkillDependencyCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a dependency between SkillId and PrerequisiteSkillId.
    /// </summary>
    public async Task<AddSkillDependencyResponse> Handle(AddSkillDependencyCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling AddSkillDependencyCommand for SkillId={SkillId}, PrereqSkillId={PrereqSkillId}", request.SkillId, request.PrerequisiteSkillId);

        // prevent duplicates
        var exists = await _repository.AnyAsync(d => d.SkillId == request.SkillId && d.PrerequisiteSkillId == request.PrerequisiteSkillId, cancellationToken);
        if (exists)
        {
            _logger.LogInformation("Add prevented: Skill dependency already exists for SkillId={SkillId}, PrereqSkillId={PrereqSkillId}", request.SkillId, request.PrerequisiteSkillId);
            throw new ConflictException("Skill dependency already exists.");
        }

        var dep = new SkillDependency
        {
            SkillId = request.SkillId,
            PrerequisiteSkillId = request.PrerequisiteSkillId,
            RelationshipType = request.RelationshipType ?? SkillRelationshipType.Prerequisite,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var created = await _repository.AddAsync(dep, cancellationToken);
        _logger.LogInformation("Created skill dependency: DepId={DepId}, SkillId={SkillId}, PrereqSkillId={PrereqSkillId}", created.Id, created.SkillId, created.PrerequisiteSkillId);
        return new AddSkillDependencyResponse
        {
            Id = created.Id,
            SkillId = created.SkillId,
            PrerequisiteSkillId = created.PrerequisiteSkillId,
            RelationshipType = created.RelationshipType,
            CreatedAt = created.CreatedAt
        };
    }
}