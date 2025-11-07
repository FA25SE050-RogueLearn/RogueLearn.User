namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Text-based summarization plugin.
/// </summary>
public interface ISummarizationPlugin
{
    Task<string> SummarizeTextAsync(string rawText, CancellationToken cancellationToken = default);
}