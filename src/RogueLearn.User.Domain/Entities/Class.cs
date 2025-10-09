using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("classes")]
public class Class : BaseEntity
{
  [Column("name")]
  public string Name { get; set; } = string.Empty;

  [Column("description")]
  public string? Description { get; set; }

  [Column("roadmap_url")]
  public string? RoadmapUrl { get; set; }

  [Column("skill_focus_areas")]
  public string[]? SkillFocusAreas { get; set; }

  [Column("difficulty_level")]
  public int DifficultyLevel { get; set; } = 1;

  [Column("estimated_duration_months")]
  public int? EstimatedDurationMonths { get; set; }

  [Column("is_active")]
  public bool IsActive { get; set; } = true;

  [Column("created_at")]
  public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

  [Column("updated_at")]
  public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}