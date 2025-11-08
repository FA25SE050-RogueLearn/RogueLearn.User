// RogueLearn.User/src/RogueLearn.User.Domain/Entities/CurriculumProgramSubject.cs
using BuildingBlocks.Shared.Common;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace RogueLearn.User.Domain.Entities;

[Table("curriculum_program_subjects")]
public class CurriculumProgramSubject : BaseEntity
{
    [PrimaryKey("program_id", shouldInsert: true)]
    public Guid ProgramId { get; set; }

    [PrimaryKey("subject_id", shouldInsert: true)]
    public Guid SubjectId { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}