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
    public string RelationshipType { get; set; } = "Prerequisite"; 
    public string Reasoning { get; set; } = string.Empty;
}