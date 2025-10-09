using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("curriculum_versions")]
public class CurriculumVersion : BaseEntity
{
    [Column("program_id")]
    public Guid ProgramId { get; set; }

    [Column("version_code")]
    public string VersionCode { get; set; } = string.Empty;

    [Column("effective_year")]
    public int EffectiveYear { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}