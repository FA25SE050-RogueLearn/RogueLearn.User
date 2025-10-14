using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("curriculum_version_activations")]
public class CurriculumVersionActivation : BaseEntity
{
    [Column("curriculum_version_id")]
    public Guid CurriculumVersionId { get; set; }

    [Column("effective_year")]
    public int EffectiveYear { get; set; }

    [Column("activated_by")]
    public Guid? ActivatedBy { get; set; }

    [Column("activated_at")]
    public DateTimeOffset ActivatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("notes")]
    public string? Notes { get; set; }
}