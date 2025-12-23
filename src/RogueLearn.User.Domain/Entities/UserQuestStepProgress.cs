using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Domain.Entities;

[Table("user_quest_step_progress")]
public class UserQuestStepProgress : BaseEntity
{
    [Column("attempt_id")]
    public Guid AttemptId { get; set; }

    [Column("step_id")]
    public Guid StepId { get; set; }

    [Column("status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public StepCompletionStatus Status { get; set; } = StepCompletionStatus.NotStarted;

    [Column("started_at")]
    public DateTimeOffset? StartedAt { get; set; }

    [Column("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    [Column("completed_activity_ids")]
    public Guid[]? CompletedActivityIds { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}