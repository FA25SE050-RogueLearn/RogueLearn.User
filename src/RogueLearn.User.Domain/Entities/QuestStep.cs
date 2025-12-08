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

    [Column("step_number")]
    public int StepNumber { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    // MODIFIED: This stores the content for THIS specific variant.
    [Column("content")]
    public object? Content { get; set; }

    [Column("validation_criteria")]
    public string? ValidationCriteria { get; set; }

    [Column("experience_points")]
    public int ExperiencePoints { get; set; } = 0;

    [Column("is_optional")]
    public bool IsOptional { get; set; } = false;

    // ADDED: The difficulty variant for this step (Supportive, Standard, Challenging)
    [Column("difficulty_variant")]
    public string DifficultyVariant { get; set; } = "Standard";

    // ADDED: The logical module grouping (e.g., Module 1 has 3 variants)
    [Column("module_number")]
    public int ModuleNumber { get; set; } = 1;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}