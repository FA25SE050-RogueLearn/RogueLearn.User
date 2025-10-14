namespace RogueLearn.User.Application.Features.Roles.Commands.CreateRole;

public class CreateRoleResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}