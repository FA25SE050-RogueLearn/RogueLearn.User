using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("user_quest_step_feedback")]
public class UserQuestStepFeedback : BaseEntity
{
    [Column("quest_id")]
    public Guid QuestId { get; set; }

    [Column("step_id")]
    public Guid StepId { get; set; }

    [Column("auth_user_id")]
    public Guid AuthUserId { get; set; }

    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    [Column("rating")]
    public int Rating { get; set; }

    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [Column("comment")]
    public string? Comment { get; set; }

    [Column("admin_notes")]
    public string? AdminNotes { get; set; }

    [Column("is_resolved")]
    public bool IsResolved { get; set; } = false;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}