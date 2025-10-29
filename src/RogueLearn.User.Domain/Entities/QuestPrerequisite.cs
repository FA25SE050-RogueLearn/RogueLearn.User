using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("quest_prerequisites")]
public class QuestPrerequisite : BaseEntity
{
    [Column("quest_id")]
    public Guid QuestId { get; set; }

    [Column("prerequisite_quest_id")]
    public Guid PrerequisiteQuestId { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}