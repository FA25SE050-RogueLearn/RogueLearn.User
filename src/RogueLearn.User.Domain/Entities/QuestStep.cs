// RogueLearn.User/src/RogueLearn.User.Domain/Entities/QuestStep.cs
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

    // REMOVED: The SkillId is no longer at the step level.
    // It will be defined for each individual activity within the 'Content' JSON.
    // public Guid SkillId { get; set; }

    [Column("step_number")]
    public int StepNumber { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    // REMOVED: The StepType is no longer needed at this level.
    // Each activity within the 'Content' will have its own type.
    // [Column("step_type")]
    // [JsonConverter(typeof(StringEnumConverter))]
    // public StepType StepType { get; set; }

    // MODIFIED: This is now a flexible object to store the new 'activities' array structure.
    // It will be serialized to the JSONB 'content' column.
    [Column("content")]
    public object? Content { get; set; }

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