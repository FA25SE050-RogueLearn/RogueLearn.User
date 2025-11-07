using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Interfaces;

public interface ITaggingSuggestionService
{
    /// <summary>
    /// Generate tag suggestions for the given user and raw text, mapping to existing tags when possible.
    /// </summary>
    Task<IReadOnlyList<TagSuggestionDto>> SuggestAsync(Guid authUserId, string rawText, int maxTags = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate tag suggestions from an uploaded file for the given user (file-first processing).
    /// </summary>
    Task<IReadOnlyList<TagSuggestionDto>> SuggestFromFileAsync(Guid authUserId, AiFileAttachment attachment, int maxTags = 10, CancellationToken cancellationToken = default);
}