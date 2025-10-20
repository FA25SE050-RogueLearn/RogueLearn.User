namespace RogueLearn.User.Application.Features.Skills.Commands.CreateSkill;

public sealed class CreateSkillResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public int Tier { get; set; }
    public string? Description { get; set; }
}