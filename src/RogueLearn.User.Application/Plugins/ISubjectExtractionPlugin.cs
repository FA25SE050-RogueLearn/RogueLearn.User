namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Defines the contract for an AI plugin that extracts structured data for a single subject from raw text.
/// </summary>
public interface ISubjectExtractionPlugin
{
    Task<string> ExtractSubjectJsonAsync(string rawSubjectText, CancellationToken cancellationToken = default);
    Task<List<string>> ExtractSkillsFromObjectivesAsync(List<string> learningObjectives, CancellationToken cancellationToken = default);
}