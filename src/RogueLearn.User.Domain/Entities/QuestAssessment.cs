using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Domain.Entities;

[Table("quest_assessments")]
public class QuestAssessment : BaseEntity
{
    [Column("quest_id")]
    public Guid QuestId { get; set; }

    [Column("assessment_type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public AssessmentType AssessmentType { get; set; }

    // JSONB fields represented as string
    [Column("configuration")]
    public string Configuration { get; set; } = string.Empty;

    [Column("passing_criteria")]
    public string PassingCriteria { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}