using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace RogueLearn.User.Application.Plugins;

public class RoadmapExtractionPlugin : IRoadmapExtractionPlugin
{
    private readonly Kernel _kernel;
    private readonly ILogger<RoadmapExtractionPlugin> _logger;

    public RoadmapExtractionPlugin(Kernel kernel, ILogger<RoadmapExtractionPlugin> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<string> ExtractClassRoadmapJsonAsync(string rawText, CancellationToken cancellationToken = default)
    {
        var header = @"Extract a software development class roadmap from the following text and return ONLY JSON following this exact schema.

{
  ""class"": {
    ""name"": ""string"",
    ""description"": ""string"",
    ""roadmapUrl"": ""string (optional)"",
    ""skillFocusAreas"": [""string""] ,
    ""difficultyLevel"": 1-5,
    ""estimatedDurationMonths"": number (optional),
    ""isActive"": true
  },
  ""nodes"": [
    {
      ""title"": ""string"",
      ""nodeType"": ""category|topic|tool|resource|checkpoint"",
      ""description"": ""string (optional)"",
      ""sequence"": number,
      ""fullPath"": ""root/Category/Subcategory/.../Title"",
      ""children"": [ { ... same schema recursively ... } ]
    }
  ]
}

RULES:
- The ""class"" object is required; infer name and description from the text (e.g., ""ASP.NET Core"").
- ""skillFocusAreas"": derive from major themes (e.g., ""Backend"", ""Web APIs"").
- ""difficultyLevel"": estimate from Beginner(1) to Expert(5).
- ""nodes"": build a hierarchical tree; set ""sequence"" starting from 1 within each sibling group.
- ""fullPath"": concatenate titles from root to current node separated by ""/""; the root starts with ""root"".
- Ensure valid JSON; escape quotes properly; do NOT include markdown fences.

Text to extract from:
";

        var prompt = header + rawText + @"

Return only the JSON object.";

        try
        {
            var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var rawResponse = result.GetValue<string>() ?? string.Empty;
            _logger.LogInformation("Roadmap extractor raw response: {RawResponse}", rawResponse);
            return CleanToJson(rawResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract class roadmap data using AI");
            return string.Empty;
        }
    }

    private static string CleanToJson(string rawResponse)
    {
        var cleanedResponse = rawResponse.Trim();
        if (cleanedResponse.StartsWith("```"))
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