// RogueLearn.User/src/RogueLearn.User.Domain/Entities/SyllabusVersion.cs
using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;
using System.Collections.Generic; // ADDED: To support Dictionary type.

namespace RogueLearn.User.Domain.Entities;

[Table("syllabus_versions")]
public class SyllabusVersion : BaseEntity
{
    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    [Column("version_number")]
    public int VersionNumber { get; set; }

    // MODIFIED: Changed the type from 'string' to 'Dictionary<string, object>?' to correctly map the JSONB column from the database.
    // This allows the Supabase client's deserializer to correctly handle the structured JSON content.
    [Column("content")]
    public Dictionary<string, object>? Content { get; set; }

    [Column("effective_date")]
    public DateOnly EffectiveDate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}