using MediatR;
using RogueLearn.User.Application.Features.Onboarding.Queries.GetAllClasses;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.ClassSpecialization.Commands.UpdateClass;

public class UpdateClassCommand : IRequest<ClassDto>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RoadmapUrl { get; set; }
    public List<string>? SkillFocusAreas { get; set; }
    public DifficultyLevel DifficultyLevel { get; set; }
    public int? EstimatedDurationMonths { get; set; }
    public bool IsActive { get; set; }
}