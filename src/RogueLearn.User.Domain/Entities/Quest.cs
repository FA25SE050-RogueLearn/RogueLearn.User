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

    // ⭐ UPDATED: Made nullable to support System-created Master Quests
    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    [Column("is_recommended")]
    public bool IsRecommended { get; set; } = false;  

    [Column("recommendation_reason")]
    public string? RecommendationReason { get; set; }

    /// <summary>
    /// Personalized difficulty based on user's academic performance for this subject.
    /// Values: Challenging (>=8.5), Standard (7.0-8.5), Supportive (&lt;7.0/failed), Adaptive (studying)
    /// </summary>
    [Column("expected_difficulty")]
    public string ExpectedDifficulty { get; set; } = "Standard";

    /// <summary>
    /// Human-readable explanation of why this difficulty was assigned.
    /// Example: "High score (8.7) - advanced content with minimal scaffolding"
    /// </summary>
    [Column("difficulty_reason")]
    public string? DifficultyReason { get; set; }

    /// <summary>
    /// Cached grade from student_semester_subjects at time of quest creation/sync.
    /// </summary>
    [Column("subject_grade")]
    public string? SubjectGrade { get; set; }

    /// <summary>
    /// Cached enrollment status: Passed, NotPassed, Studying, NotStarted
    /// </summary>
    [Column("subject_status")]
    public string? SubjectStatus { get; set; }

}
