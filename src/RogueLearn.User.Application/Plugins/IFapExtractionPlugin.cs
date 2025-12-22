namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Defines the contract for a plugin that extracts structured data from FAP (FPT University Academic Portal) content.
/// </summary>
public interface IFapExtractionPlugin
{
    Task<string> ExtractFapRecordJsonAsync(string rawTranscriptText, CancellationToken cancellationToken = default);
}