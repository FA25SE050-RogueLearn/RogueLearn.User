using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// File-first summarization plugin.
/// </summary>
public interface IFileSummarizationPlugin
{
    // Returns a structured BlockNote document (typically a top-level array of blocks)
    // that can be serialized as JSONB. Null indicates summarization failed.
    Task<object?> SummarizeAsync(AiFileAttachment attachment, CancellationToken cancellationToken = default);
}