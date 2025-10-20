namespace RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;

public class IngestXpEventResponse
{
    public bool Processed { get; set; }
    public Guid? RewardId { get; set; }
    public string? Message { get; set; }

    // Optional summary
    public string SkillName { get; set; } = string.Empty;
    public int NewExperiencePoints { get; set; }
    public int NewLevel { get; set; }
}