namespace RogueLearn.User.Application.Features.Roles.Queries.GetAllRoles;

public class GetAllRolesResponse
{
    public List<RoleDto> Roles { get; set; } = new();
}

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}