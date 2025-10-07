using BuildingBlocks.Shared.Common;
using RogueLearn.User.Domain.Enums;
using Supabase.Postgrest.Attributes;

namespace RogueLearn.User.Domain.Entities;

[Table("curriculum_programs")]
public class CurriculumProgram : BaseEntity
{
    [Column("program_name")]
    public string ProgramName { get; set; } = string.Empty;

    [Column("program_code")]
    public string ProgramCode { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("degree_level")]
    public DegreeLevel DegreeLevel { get; set; }

    [Column("total_credits")]
    public int? TotalCredits { get; set; }

    [Column("duration_years")]
    public int? DurationYears { get; set; }
}