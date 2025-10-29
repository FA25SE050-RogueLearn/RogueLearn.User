using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("roles")]
public class Role : BaseEntity
{
	[Column("name")]
	public string Name { get; set; } = string.Empty;

	[Column("description")]
	public string? Description { get; set; }

	[Column("permissions")]
	public Dictionary<string, object>? Permissions { get; set; }

	[Column("created_at")]
	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

	[Column("updated_at")]
	public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}