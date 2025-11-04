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

    // MODIFIED: The method is renamed and now accepts a list of objectives to process them in a single batch.
    /// <summary>
    /// Analyzes a list of descriptive learning objectives and extracts the primary, concise skill name for each.
    /// </summary>
    /// <param name="learningObjectives">A list of full learning objective sentences.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of extracted skill names, corresponding to the input list order.</returns>
    Task<List<string>> ExtractSkillsFromObjectivesAsync(List<string> learningObjectives, CancellationToken cancellationToken = default);
}