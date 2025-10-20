namespace RogueLearn.User.Application.Features.SkillDependencies.Queries.GetSkillDependencies;

public sealed class GetSkillDependenciesResponse
{
    public List<SkillDependencyDto> Dependencies { get; set; } = new();
}