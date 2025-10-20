namespace RogueLearn.User.Application.Features.Skills.Queries.GetSkills;

public sealed class GetSkillsResponse
{
    public List<SkillDto> Skills { get; set; } = new();
}