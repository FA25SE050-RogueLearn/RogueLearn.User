namespace RogueLearn.User.Application.Features.UserSkills.Queries.GetUserSkill;

public sealed class GetUserSkillResponse
{
    public string SkillName { get; set; } = string.Empty;
    public int ExperiencePoints { get; set; }
    public int Level { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; }
}