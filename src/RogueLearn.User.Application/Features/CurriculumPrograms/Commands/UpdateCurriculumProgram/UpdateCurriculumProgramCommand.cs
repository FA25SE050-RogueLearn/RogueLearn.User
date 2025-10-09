using MediatR;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.CurriculumPrograms.Commands.UpdateCurriculumProgram;

public class UpdateCurriculumProgramCommand : IRequest<UpdateCurriculumProgramResponse>
{
    public Guid Id { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string ProgramCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DegreeLevel DegreeLevel { get; set; }
    public int? TotalCredits { get; set; }
    public int? DurationYears { get; set; }
}