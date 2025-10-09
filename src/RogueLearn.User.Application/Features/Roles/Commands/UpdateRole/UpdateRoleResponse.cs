namespace RogueLearn.User.Application.Features.Roles.Commands.UpdateRole;

public class UpdateRoleResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; }
}