using MediatR;

namespace RogueLearn.User.Application.Features.SkillDependencies.Queries.GetSkillDependencies;

public sealed class GetSkillDependenciesQuery : IRequest<GetSkillDependenciesResponse>
{
    public Guid SkillId { get; set; }
}