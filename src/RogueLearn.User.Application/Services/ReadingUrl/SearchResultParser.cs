namespace RogueLearn.User.Application.Services;

public static class SearchResultParser
{
    public static string? ExtractUrlFromSearchResult(string searchResult)
    {
        var lines = searchResult.Split('\n');
        var urlLine = lines.FirstOrDefault(line =>
            line.StartsWith("Link: ", StringComparison.OrdinalIgnoreCase));

        return urlLine?.Substring("Link: ".Length).Trim();
    }
}
