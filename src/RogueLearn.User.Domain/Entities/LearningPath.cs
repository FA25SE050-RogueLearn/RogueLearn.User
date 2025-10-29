using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Domain.Entities;

[Table("learning_paths")]
public class LearningPath : BaseEntity
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("path_type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public PathType PathType { get; set; }

    [Column("curriculum_version_id")]
    public Guid? CurriculumVersionId { get; set; }

    [Column("estimated_total_duration_hours")]
    public int? EstimatedTotalDurationHours { get; set; }

    [Column("total_experience_points")]
    public int TotalExperiencePoints { get; set; } = 0;

    [Column("is_published")]
    public bool IsPublished { get; set; } = false;

    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}