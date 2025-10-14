using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Domain.Entities;

[Table("user_quest_progress")]
public class UserQuestProgress : BaseEntity
{
    [Column("auth_user_id")]
    public Guid AuthUserId { get; set; }

    [Column("quest_id")]
    public Guid QuestId { get; set; }

    [Column("status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public QuestStatus Status { get; set; } = QuestStatus.NotStarted;

    [Column("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    [Column("last_updated_at")]
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}