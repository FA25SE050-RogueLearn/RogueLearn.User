namespace RogueLearn.User.Application.Features.UserSkills.Queries.GetUserSkills;

public sealed class GetUserSkillsResponse
{
    public List<UserSkillDto> Skills { get; set; } = new();
}