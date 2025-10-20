namespace RogueLearn.User.Application.Features.UserSkills.Commands.AddUserSkill;

public sealed class AddUserSkillResponse
{
    public Guid Id { get; set; }
    public Guid AuthUserId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int ExperiencePoints { get; set; }
    public int Level { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; }
}