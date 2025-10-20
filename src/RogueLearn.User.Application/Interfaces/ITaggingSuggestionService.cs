using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Interfaces;

public interface ITaggingSuggestionService
{
    /// <summary>
    /// Generate tag suggestions for the given user and raw text, mapping to existing tags when possible.
    /// </summary>
    Task<IReadOnlyList<TagSuggestionDto>> SuggestAsync(Guid authUserId, string rawText, int maxTags = 10, CancellationToken cancellationToken = default);
}