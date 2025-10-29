using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Domain.Entities;

[Table("learning_path_quests")]
public class LearningPathQuest : BaseEntity
{
    [Column("learning_path_id")]
    public Guid LearningPathId { get; set; }

    [Column("quest_id")]
    public Guid QuestId { get; set; }

    [Column("difficulty_level")]
    [JsonConverter(typeof(StringEnumConverter))]
    public DifficultyLevel DifficultyLevel { get; set; }

    [Column("sequence_order")]
    public int SequenceOrder { get; set; }

    [Column("is_mandatory")]
    public bool IsMandatory { get; set; } = true;

    // Stored as JSONB; represented as string
    [Column("unlock_criteria")]
    public string? UnlockCriteria { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}