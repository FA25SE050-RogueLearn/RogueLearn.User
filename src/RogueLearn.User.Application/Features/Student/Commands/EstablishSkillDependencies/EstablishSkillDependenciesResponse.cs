// RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Commands/EstablishSkillDependencies/EstablishSkillDependenciesResponse.cs
namespace RogueLearn.User.Application.Features.Student.Commands.EstablishSkillDependencies;

public class EstablishSkillDependenciesResponse
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalDependenciesCreated { get; set; }
    public int TotalDependenciesSkipped { get; set; }
    public List<SkillDependencyInfo> Dependencies { get; set; } = new();
}

public class SkillDependencyInfo
{
    public string SkillName { get; set; } = string.Empty;
    public string PrerequisiteSkillName { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
}