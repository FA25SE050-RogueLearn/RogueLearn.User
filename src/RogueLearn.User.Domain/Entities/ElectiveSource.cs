using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("elective_sources")]
public class ElectiveSource : BaseEntity
{
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("url")]
    public string Url { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("submitted_by")]
    public Guid? SubmittedBy { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Pending";

    [Column("submitted_at")]
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("reviewed_at")]
    public DateTimeOffset? ReviewedAt { get; set; }

    [Column("reviewer_id")]
    public Guid? ReviewerId { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }
}