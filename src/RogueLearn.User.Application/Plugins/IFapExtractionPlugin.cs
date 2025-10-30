// RogueLearn.User/src/RogueLearn.User.Application/Plugins/IFapExtractionPlugin.cs
namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Defines the contract for a plugin that extracts structured data from FAP (FPT University Academic Portal) content.
/// </summary>
public interface IFapExtractionPlugin
{
    /// <summary>
    /// Extracts academic record data from pre-processed text and returns it as a structured JSON string.
    /// </summary>
    /// <param name="rawTranscriptText">The cleaned text content from the student's transcript table.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A JSON string matching the FapRecordData schema.</returns>
    Task<string> ExtractFapRecordJsonAsync(string rawTranscriptText, CancellationToken cancellationToken = default);
}