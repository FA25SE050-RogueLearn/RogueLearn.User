// RogueLearn.User/src/RogueLearn.User.Application/Plugins/FapExtractionPlugin.cs
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Threading;
using System.Threading.Tasks;

namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Implementation of IFapExtractionPlugin using Semantic Kernel to call a generative AI model (e.g., Gemini).
/// </summary>
public class FapExtractionPlugin : IFapExtractionPlugin
{
    private readonly Kernel _kernel;
    private readonly ILogger<FapExtractionPlugin> _logger;

    public FapExtractionPlugin(Kernel kernel, ILogger<FapExtractionPlugin> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<string> ExtractFapRecordJsonAsync(string rawTranscriptText, CancellationToken cancellationToken = default)
    {
        var header = @"
Analyze the following academic transcript text and extract the grade information for each subject. Return it as JSON following this exact schema:

{
  ""gpa"": number (optional, find if present),
  ""subjects"": [
    {
      ""subjectCode"": ""string (e.g., 'CSI104')"",
      ""status"": ""string (must be one of 'Passed', 'Failed', 'Studying')"",
      ""mark"": number (optional, extract the numeric grade if available)
    }
  ]
}

Important Rules:
- The input is the clean text from a table. Each row's data will appear sequentially.
- Identify the Subject Code, the numeric Grade/Mark, and the final Status for each row.
- Map the status text directly to 'Passed', 'Failed', or 'Studying'.
- Ignore header rows or rows that do not contain a subject code.
- If a grade is not present for a subject (e.g., for 'Studying' courses), the 'mark' property should be null.
- Return ONLY the JSON object, with no additional text or markdown formatting.

Text to extract from:
";

        var prompt = header + rawTranscriptText + @"

Return only the JSON, no additional text or formatting.";

        try
        {
            var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var rawResponse = result.GetValue<string>() ?? string.Empty;
            _logger.LogInformation("FAP Record extractor raw response: {RawResponse}", rawResponse);
            return CleanToJson(rawResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract academic record data using AI");
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