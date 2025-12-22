using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Domain.Entities;

[Table("user_quest_attempts")]
public class UserQuestAttempt : BaseEntity
{
    [Column("auth_user_id")]
    public Guid AuthUserId { get; set; }

    [Column("quest_id")]
    public Guid QuestId { get; set; }

    [Column("status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public QuestAttemptStatus Status { get; set; } = QuestAttemptStatus.InProgress;

    [Column("started_at")]
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    [Column("abandoned_at")]
    public DateTimeOffset? AbandonedAt { get; set; }

    [Column("total_experience_earned")]
    public int TotalExperienceEarned { get; set; } = 0;

    [Column("completion_percentage")]
    public decimal CompletionPercentage { get; set; } = 0.00m;
    [Column("current_step_id")]
    public Guid? CurrentStepId { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("assigned_difficulty")]
    public string AssignedDifficulty { get; set; } = "Standard";

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}