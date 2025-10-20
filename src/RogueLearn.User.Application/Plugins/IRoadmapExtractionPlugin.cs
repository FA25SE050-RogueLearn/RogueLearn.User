namespace RogueLearn.User.Application.Plugins;

public interface IRoadmapExtractionPlugin
{
    /// <summary>
    /// Extracts a normalized Class Roadmap JSON from raw text (e.g., PDF text from roadmap.sh).
    /// The JSON must contain a root "class" object and a "nodes" array with hierarchical nodes.
    /// </summary>
    /// <param name="rawText">Raw text content extracted from roadmap.pdf</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON string following the normalized schema</returns>
    Task<string> ExtractClassRoadmapJsonAsync(string rawText, CancellationToken cancellationToken = default);
}