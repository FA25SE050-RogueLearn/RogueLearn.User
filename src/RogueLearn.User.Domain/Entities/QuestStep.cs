using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Domain.Entities;

[Table("quest_steps")]
public class QuestStep : BaseEntity
{
    [Column("quest_id")]
    public Guid QuestId { get; set; }
    [Column("skill_id")]
    public Guid SkillId { get; set; }

    [Column("step_number")]
    public int StepNumber { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("step_type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public StepType StepType { get; set; }

    // Stored as JSONB; represented as string
    [Column("content")]
    public string? Content { get; set; }

    // Stored as JSONB; represented as string
    [Column("validation_criteria")]
    public string? ValidationCriteria { get; set; }

    [Column("experience_points")]
    public int ExperiencePoints { get; set; } = 0;

    [Column("is_optional")]
    public bool IsOptional { get; set; } = false;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}