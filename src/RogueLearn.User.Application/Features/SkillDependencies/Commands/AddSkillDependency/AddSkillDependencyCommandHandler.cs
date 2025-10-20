using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.SkillDependencies.Commands.AddSkillDependency;

public sealed class AddSkillDependencyCommandHandler : IRequestHandler<AddSkillDependencyCommand, AddSkillDependencyResponse>
{
    private readonly ISkillDependencyRepository _repository;

    public AddSkillDependencyCommandHandler(ISkillDependencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<AddSkillDependencyResponse> Handle(AddSkillDependencyCommand request, CancellationToken cancellationToken)
    {
        // prevent duplicates
        var exists = await _repository.AnyAsync(d => d.SkillId == request.SkillId && d.PrerequisiteSkillId == request.PrerequisiteSkillId, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("Skill dependency already exists.");
        }

        var dep = new SkillDependency
        {
            SkillId = request.SkillId,
            PrerequisiteSkillId = request.PrerequisiteSkillId,
            RelationshipType = string.IsNullOrWhiteSpace(request.RelationshipType) ? "Prerequisite" : request.RelationshipType!,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var created = await _repository.AddAsync(dep, cancellationToken);
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