namespace RogueLearn.User.Application.Plugins;

public interface ITagSuggestionPlugin
{
    /// <summary>
    /// Generate tag suggestions for the given raw text and return ONLY JSON string.
    /// Expected JSON schema:
    /// {
    ///   "tags": [
    ///     { "name": "string", "confidence": 0.0, "reason": "string" }
    ///   ]
    /// }
    /// </summary>
    Task<string> GenerateTagSuggestionsJsonAsync(string rawText, int maxTags = 10, CancellationToken cancellationToken = default);
}