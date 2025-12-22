namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Text-based summarization plugin.
/// </summary>
public interface ISummarizationPlugin
{
    Task<object?> SummarizeTextAsync(string rawText, CancellationToken cancellationToken = default);
}