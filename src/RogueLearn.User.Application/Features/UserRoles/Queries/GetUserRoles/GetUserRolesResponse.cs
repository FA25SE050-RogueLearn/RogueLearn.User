namespace RogueLearn.User.Application.Features.UserRoles.Queries.GetUserRoles;

public class GetUserRolesResponse
{
    public Guid UserId { get; set; }
    public List<UserRoleDto> Roles { get; set; } = new();
}

public class UserRoleDto
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
}