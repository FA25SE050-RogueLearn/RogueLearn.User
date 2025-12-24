using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

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

    [Column("content")]
    public object? Content { get; set; }

    [Column("experience_points")]
    public int ExperiencePoints { get; set; } = 0;

    [Column("is_optional")]
    public bool IsOptional { get; set; } = false;

    [Column("difficulty_variant")]
    public string DifficultyVariant { get; set; } = "Standard";

    [Column("module_number")]
    public int ModuleNumber { get; set; } = 1;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}