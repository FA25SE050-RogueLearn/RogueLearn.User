// RogueLearn.User/src/RogueLearn.User.Application/Plugins/ISkillDependencyAnalysisPlugin.cs
namespace RogueLearn.User.Application.Plugins;

public interface ISkillDependencyAnalysisPlugin
{
    Task<List<SkillDependencyAnalysis>> AnalyzeSkillDependenciesAsync(
        List<string> skillNames,
        CancellationToken cancellationToken);
}

public class SkillDependencyAnalysis
{
    public string SkillName { get; set; } = string.Empty;
    public string PrerequisiteSkillName { get; set; } = string.Empty;

    // MODIFICATION: Added the missing RelationshipType property.
    // This property will hold the relationship type suggested by the AI.
    public string RelationshipType { get; set; } = "Prerequisite"; // Prerequisite, Complements, Alternative

    public string Reasoning { get; set; } = string.Empty;
}