using MediatR;

namespace RogueLearn.User.Application.Features.Skills.Commands.UpdateSkill;

public sealed class UpdateSkillCommand : IRequest<UpdateSkillResponse>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public int Tier { get; set; } = 1;
    public string? Description { get; set; }
}