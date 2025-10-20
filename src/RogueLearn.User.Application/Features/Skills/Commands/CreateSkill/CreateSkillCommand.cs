using MediatR;

namespace RogueLearn.User.Application.Features.Skills.Commands.CreateSkill;

public sealed class CreateSkillCommand : IRequest<CreateSkillResponse>
{
    public string Name { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public int Tier { get; set; } = 1;
    public string? Description { get; set; }
}