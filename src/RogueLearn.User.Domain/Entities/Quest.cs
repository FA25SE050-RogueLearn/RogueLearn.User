using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Domain.Entities;

[Table("quests")]
public class Quest : BaseEntity
{
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("quest_type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public QuestType QuestType { get; set; }

    [Column("difficulty_level")]
    [JsonConverter(typeof(StringEnumConverter))]
    public DifficultyLevel DifficultyLevel { get; set; }

    [Column("quest_status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public QuestStatus Status { get; set; } = QuestStatus.Draft;

    [Column("subject_id")]
    public Guid? SubjectId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    [Column("is_recommended")]
    public bool IsRecommended { get; set; } = false;

    [Column("expected_difficulty")]
    public string ExpectedDifficulty { get; set; } = "Standard";

    [Column("difficulty_reason")]
    public string? DifficultyReason { get; set; }

}