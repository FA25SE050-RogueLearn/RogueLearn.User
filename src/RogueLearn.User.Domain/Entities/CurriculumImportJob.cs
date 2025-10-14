using BuildingBlocks.Shared.Common;

using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("curriculum_import_jobs")]
public class CurriculumImportJob : BaseEntity
{
    [Column("source_type")]
    public string SourceType { get; set; } = string.Empty;

    [Column("program_code")]
    public string ProgramCode { get; set; } = string.Empty;

    [Column("file_url")]
    public string? FileUrl { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }
}