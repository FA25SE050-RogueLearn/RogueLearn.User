using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Domain.Entities;

[Table("quest_chapters")]
public class QuestChapter : BaseEntity
{
    [Column("learning_path_id")]
    public Guid LearningPathId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("sequence")]
    public int Sequence { get; set; }

    [Column("status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public PathProgressStatus Status { get; set; } = PathProgressStatus.NotStarted;

    [Column("start_date")]
    public DateOnly? StartDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}