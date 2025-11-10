// RogueLearn.User/src/RogueLearn.User.Domain/Entities/Quest.cs
using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Domain.Entities;

[Table("quests")]
public class Quest : BaseEntity
{
    [Column("quest_chapter_id")]
    public Guid? QuestChapterId { get; set; }

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

    [Column("estimated_duration_minutes")]
    public int? EstimatedDurationMinutes { get; set; }

    [Column("experience_points_reward")]
    public int ExperiencePointsReward { get; set; } = 0;

    [Column("sequence")]
    public int? Sequence { get; set; } // CHANGED: Made nullable

    [Column("quest_status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public QuestStatus Status { get; set; } = QuestStatus.NotStarted;

    [Column("subject_id")]
    public Guid? SubjectId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}