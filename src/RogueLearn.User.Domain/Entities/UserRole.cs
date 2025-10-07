using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("user_roles")]
public class UserRole : BaseEntity
{
	[Column("auth_user_id")]
	public Guid AuthUserId { get; set; }

	[Column("role_id")]
	public Guid RoleId { get; set; }

	[Column("assigned_at")]
	public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}