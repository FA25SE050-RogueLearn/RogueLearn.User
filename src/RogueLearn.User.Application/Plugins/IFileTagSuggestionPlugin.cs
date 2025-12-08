using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// File-first tag suggestion plugin, generates ONLY JSON tag suggestions from an attached file.
/// </summary>
public interface IFileTagSuggestionPlugin
{
    Task<string> GenerateTagSuggestionsJsonAsync(AiFileAttachment attachment, int maxTags = 10, CancellationToken cancellationToken = default);
    Task<string> GenerateTagSuggestionsJsonAsync(AiFileAttachment attachment, IEnumerable<string> knownTags, int maxTags = 10, CancellationToken cancellationToken = default);
}