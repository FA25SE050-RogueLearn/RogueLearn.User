// RogueLearn.User/src/RogueLearn.User.Domain/Entities/Subject.cs
using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;
using System.Collections.Generic;

namespace RogueLearn.User.Domain.Entities;

[Table("subjects")]
public class Subject : BaseEntity
{
    [Column("subject_code")]
    public string SubjectCode { get; set; } = string.Empty;

    [Column("subject_name")]
    public string SubjectName { get; set; } = string.Empty;

    [Column("credits")]
    public int Credits { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    // MODIFIED: This property is reverted to Dictionary<string, object> to work with the new deep deserialization logic.
    [Column("content")]
    public Dictionary<string, object>? Content { get; set; }

    [Column("semester")]
    public int? Semester { get; set; }

    [Column("prerequisite_subject_ids")]
    public Guid[]? PrerequisiteSubjectIds { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}