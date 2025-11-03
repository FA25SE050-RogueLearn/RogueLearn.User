namespace RogueLearn.User.Application.Features.UserSkills.Queries.GetUserSkills;

public sealed class UserSkillDto
{
    // ADDED: The SkillId from the user_skills table is now included in the response.
    // This provides a stable reference to the master skill in the 'skills' table.
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int ExperiencePoints { get; set; }
    public int Level { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; }
}