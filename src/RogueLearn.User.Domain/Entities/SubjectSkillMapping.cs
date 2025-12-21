using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("subject_skill_mappings")]
public class SubjectSkillMapping : BaseEntity
{
    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    [Column("skill_id")]
    public Guid SkillId { get; set; }

    [Column("relevance_weight")]
    public decimal RelevanceWeight { get; set; } = 1.00m;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}