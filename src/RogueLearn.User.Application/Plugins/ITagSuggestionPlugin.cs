namespace RogueLearn.User.Application.Plugins;

public interface ITagSuggestionPlugin
{
    Task<string> GenerateTagSuggestionsJsonAsync(string rawText, int maxTags = 10, CancellationToken cancellationToken = default);
}