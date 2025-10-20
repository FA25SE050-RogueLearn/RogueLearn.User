namespace RogueLearn.User.Application.Features.UserSkills.Queries.GetUserSkills;

public sealed class UserSkillDto
{
    public string SkillName { get; set; } = string.Empty;
    public int ExperiencePoints { get; set; }
    public int Level { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; }
}