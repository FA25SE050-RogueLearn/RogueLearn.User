using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Domain.Entities;

[Table("lecturer_verification_requests")]
public class LecturerVerificationRequest : BaseEntity
{
    [Column("auth_user_id")]
    public Guid AuthUserId { get; set; }

    [Column("institution_name")]
    public string InstitutionName { get; set; } = string.Empty;

    [Column("department")]
    public string? Department { get; set; }

    [Column("verification_document_url")]
    public string? VerificationDocumentUrl { get; set; }

    [Column("status")]
    [JsonConverter(typeof(StringEnumConverter))]
    public VerificationStatus Status { get; set; } = VerificationStatus.Pending;

    [Column("submitted_at")]
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("reviewed_at")]
    public DateTimeOffset? ReviewedAt { get; set; }

    [Column("reviewer_id")]
    public Guid? ReviewerId { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }
}