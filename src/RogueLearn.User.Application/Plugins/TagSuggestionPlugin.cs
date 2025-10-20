using System;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace RogueLearn.User.Application.Plugins;

public class TagSuggestionPlugin : ITagSuggestionPlugin
{
    private readonly Kernel _kernel;
    private readonly ILogger<TagSuggestionPlugin> _logger;

    public TagSuggestionPlugin(Kernel kernel, ILogger<TagSuggestionPlugin> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<string> GenerateTagSuggestionsJsonAsync(string rawText, int maxTags = 10, CancellationToken cancellationToken = default)
    {
        var clampedMax = Math.Max(1, Math.Min(20, maxTags));
        var header = $@"Analyze the following text and propose a concise list of topic tags. Return ONLY JSON following this exact schema:

{{
  ""tags"": [
    {{ ""label"": ""string"", ""confidence"": number (0.0-1.0), ""reason"": ""short explanation (<=120 chars)"" }}
  ]
}}

Rules:
- Use 1-{clampedMax} tags max, ordered by confidence.
- Prefer simple, commonly-used labels users would recognize (e.g., 'C#', 'ASP.NET Core', 'JWT').
- Avoid duplicates and near-duplicates: normalize case, treat hyphen/space variations as same (e.g., dotnet, .NET).
- Do NOT include markdown fences or commentary.

Text to analyze:
";

        var prompt = header + rawText + "\n\nReturn only the JSON object.";

        try
        {
            var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var rawResponse = result.GetValue<string>() ?? string.Empty;
            _logger.LogInformation("Tag suggestion raw response: {RawResponse}", rawResponse);
            return CleanToJson(rawResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate tag suggestions using AI");
            // Return minimal empty structure to avoid parse failures
            return JsonSerializer.Serialize(new { tags = Array.Empty<object>() });
        }
    }

    private static string CleanToJson(string rawResponse)
    {
        var cleanedResponse = rawResponse.Trim();
        if (cleanedResponse.StartsWith("```") )
        {
            var firstNewline = cleanedResponse.IndexOf('\n');
            if (firstNewline > -1)
            {
                cleanedResponse = cleanedResponse[(firstNewline + 1)..];
            }
        }
        if (cleanedResponse.EndsWith("```") && cleanedResponse.Length >= 3)
        {
            var lastFenceIndex = cleanedResponse.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFenceIndex > -1)
            {
                cleanedResponse = cleanedResponse[..lastFenceIndex];
            }
        }
        var startIdx = cleanedResponse.IndexOf('{');
        var endIdx = cleanedResponse.LastIndexOf('}');
        if (startIdx >= 0 && endIdx > startIdx)
        {
            cleanedResponse = cleanedResponse.Substring(startIdx, endIdx - startIdx + 1);
        }
        return cleanedResponse.Trim();
    }
}