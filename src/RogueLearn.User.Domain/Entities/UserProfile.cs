using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("user_profiles")]
public class UserProfile : BaseEntity
{
	[Column("auth_user_id")]
	public Guid AuthUserId { get; set; }

	[Column("username")]
	public string Username { get; set; } = string.Empty;

	[Column("email")]
	public string Email { get; set; } = string.Empty;

	[Column("first_name")]
	public string? FirstName { get; set; }

	[Column("last_name")]
	public string? LastName { get; set; }

	[Column("class_id")]
	public Guid? ClassId { get; set; }

	[Column("route_id")]
	public Guid? RouteId { get; set; }

	[Column("level")]
	public int Level { get; set; } = 1;

	[Column("experience_points")]
	public int ExperiencePoints { get; set; } = 0;

	[Column("profile_image_url")]
	public string? ProfileImageUrl { get; set; }

	[Column("onboarding_completed")]
	public bool OnboardingCompleted { get; set; } = false;

	[Column("created_at")]
	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

	[Column("updated_at")]
	public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}