// RogueLearn.User/src/RogueLearn.User.Application/Features/Onboarding/Queries/GetAllClasses/ClassDto.cs
namespace RogueLearn.User.Application.Features.Onboarding.Queries.GetAllClasses;

public class ClassDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string[]? SkillFocusAreas { get; set; }
    public string? Description { get; set; }
    public string? RoadmapUrl { get; set; }
}