using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("user_quest_progress")]
public class UserQuestProgress : BaseEntity
{
    [Column("auth_user_id")]
    public Guid AuthUserId { get; set; }

    [Column("quest_id")]
    public Guid QuestId { get; set; }

    [Column("status")]
    public QuestStatus Status { get; set; } = QuestStatus.NotStarted;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("last_updated_at")]
    public DateTime LastUpdatedAt { get; set; }
}