using RogueLearn.User.Domain.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms;

public class CurriculumProgramDto
{
    public Guid Id { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string ProgramCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    [JsonConverter(typeof(StringEnumConverter))]
    public DegreeLevel DegreeLevel { get; set; }
    
    public int? TotalCredits { get; set; }
    public int? DurationYears { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}