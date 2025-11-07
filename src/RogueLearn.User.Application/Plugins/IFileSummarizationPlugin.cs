using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// File-first summarization plugin.
/// </summary>
public interface IFileSummarizationPlugin
{
    Task<string> SummarizeAsync(AiFileAttachment attachment, CancellationToken cancellationToken = default);
}