using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.SkillDependencies.Queries.GetSkillDependencies;

public sealed class GetSkillDependenciesQueryHandler : IRequestHandler<GetSkillDependenciesQuery, GetSkillDependenciesResponse>
{
    private readonly ISkillDependencyRepository _repository;

    public GetSkillDependenciesQueryHandler(ISkillDependencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetSkillDependenciesResponse> Handle(GetSkillDependenciesQuery request, CancellationToken cancellationToken)
    {
        var deps = await _repository.FindAsync(d => d.SkillId == request.SkillId, cancellationToken);
        return new GetSkillDependenciesResponse
        {
            Dependencies = deps.Select(d => new SkillDependencyDto
            {
                Id = d.Id,
                SkillId = d.SkillId,
                PrerequisiteSkillId = d.PrerequisiteSkillId,
                RelationshipType = d.RelationshipType,
                CreatedAt = d.CreatedAt
            }).ToList()
        };
    }
}