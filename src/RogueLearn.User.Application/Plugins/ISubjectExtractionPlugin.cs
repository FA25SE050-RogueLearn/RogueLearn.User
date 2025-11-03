// RogueLearn.User/src/RogueLearn.User.Application/Plugins/ISubjectExtractionPlugin.cs
namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Defines the contract for an AI plugin that extracts structured data for a single subject from raw text.
/// </summary>
public interface ISubjectExtractionPlugin
{
    /// <summary>
    /// Extracts a single academic subject's data from pre-processed text and returns it as a structured JSON string.
    /// </summary>
    /// <param name="rawSubjectText">The cleaned text content describing a single subject.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A JSON string matching the SubjectData schema used in CurriculumImport.</returns>
    Task<string> ExtractSubjectJsonAsync(string rawSubjectText, CancellationToken cancellationToken = default);

    // ADDED: A new method contract to extract a concise skill name from a verbose learning objective.
    /// <summary>
    /// Analyzes a descriptive learning objective and extracts the primary, concise skill name.
    /// </summary>
    /// <param name="learningObjectiveText">The full text of the learning objective (e.g., "To master the contents regarding...").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The extracted skill name (e.g., "Scientific Socialism") or an empty string if none is found.</returns>
    Task<string> ExtractSkillFromObjectiveAsync(string learningObjectiveText, CancellationToken cancellationToken = default);
}