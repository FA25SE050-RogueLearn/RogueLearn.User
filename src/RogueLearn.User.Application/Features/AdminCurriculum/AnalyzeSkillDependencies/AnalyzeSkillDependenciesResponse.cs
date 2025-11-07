// RogueLearn.User/src/RogueLearn.User.Application/Features/AdminCurriculum/AnalyzeSkillDependencies/AnalyzeSkillDependenciesResponse.cs
namespace RogueLearn.User.Application.Features.AdminCurriculum.AnalyzeSkillDependencies;

public class AnalyzeSkillDependenciesResponse
{
    public string Message { get; set; } = string.Empty;
    public List<SuggestedDependencyDto> SuggestedDependencies { get; set; } = new();
}

public class SuggestedDependencyDto
{
    public string SkillName { get; set; } = string.Empty;
    public string PrerequisiteSkillName { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
}
