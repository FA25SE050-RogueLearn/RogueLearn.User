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

	[Column("created_at")]
	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}