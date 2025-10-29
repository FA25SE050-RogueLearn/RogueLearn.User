using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Domain.Entities;

[Table("user_learning_path_progress")]
public class UserLearningPathProgress : BaseEntity
{
    [Column("auth_user_id")]
    public Guid AuthUserId { get; set; }

    [Column("learning_path_id")]
    public Guid LearningPathId { get; set; }

    [Column("status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public PathProgressStatus Status { get; set; } = PathProgressStatus.NotStarted;

    [Column("started_at")]
    public DateTimeOffset? StartedAt { get; set; }

    [Column("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    [Column("current_quest_id")]
    public Guid? CurrentQuestId { get; set; }

    [Column("completed_quests_count")]
    public int CompletedQuestsCount { get; set; } = 0;

    [Column("total_quests_count")]
    public int TotalQuestsCount { get; set; } = 0;

    [Column("completion_percentage")]
    public decimal CompletionPercentage { get; set; } = 0.00m;

    [Column("total_experience_earned")]
    public int TotalExperienceEarned { get; set; } = 0;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}