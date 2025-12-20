using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Plugins;

public interface IAcademicAnalysisPlugin
{
    Task<AcademicAnalysisReport> AnalyzePerformanceAsync(
        List<FapSubjectData> extractedGrades,
        Dictionary<string, string> subjectNames, // Code -> Name map
        CancellationToken cancellationToken = default);
}