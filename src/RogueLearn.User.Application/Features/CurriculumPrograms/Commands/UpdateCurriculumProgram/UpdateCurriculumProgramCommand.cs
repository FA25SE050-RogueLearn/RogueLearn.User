using MediatR;
using RogueLearn.User.Domain.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RogueLearn.User.Application.Features.CurriculumPrograms.Commands.UpdateCurriculumProgram;

public class UpdateCurriculumProgramCommand : IRequest<UpdateCurriculumProgramResponse>
{
    public Guid Id { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string ProgramCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    [JsonConverter(typeof(StringEnumConverter))]
    public DegreeLevel DegreeLevel { get; set; }
    
    public int? TotalCredits { get; set; }
    public int? DurationYears { get; set; }
}