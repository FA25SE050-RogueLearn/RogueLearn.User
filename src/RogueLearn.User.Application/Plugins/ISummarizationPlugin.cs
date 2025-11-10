namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Text-based summarization plugin.
/// </summary>
public interface ISummarizationPlugin
{
    // Returns a structured BlockNote document (typically a top-level array of blocks)
    // that can be serialized as JSONB. Null indicates summarization failed.
    Task<object?> SummarizeTextAsync(string rawText, CancellationToken cancellationToken = default);
}