using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace RogueLearn.User.Application.Plugins;

public class CurriculumExtractionPlugin : ICurriculumExtractionPlugin
{
    private readonly Kernel _kernel;
    private readonly ILogger<CurriculumExtractionPlugin> _logger;
    private readonly string _promptPath;

    public CurriculumExtractionPlugin(Kernel kernel, ILogger<CurriculumExtractionPlugin> logger)
    {
        _kernel = kernel;
        _logger = logger;
        _promptPath = Path.Combine(AppContext.BaseDirectory, "Features", "CurriculumImport", "Prompts", "ExtractCurriculumPrompt.txt");
    }

    public async Task<string> ExtractCurriculumJsonAsync(string rawText, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_promptPath))
            {
                _logger.LogError("Critical Error: Curriculum prompt file not found at {Path}", _promptPath);
                throw new FileNotFoundException("The curriculum prompt template file is missing.", _promptPath);
            }
            var promptTemplate = await File.ReadAllTextAsync(_promptPath, cancellationToken);
            var prompt = promptTemplate.Replace("{{RAW_TEXT}}", rawText);

            var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var rawResponse = result.GetValue<string>() ?? string.Empty;
            _logger.LogInformation("Curriculum extractor raw response: {RawResponse}", rawResponse);
            return CleanToJson(rawResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract curriculum data using AI");
            return string.Empty;
        }
    }

    private static string CleanToJson(string rawResponse)
    {
        var cleanedResponse = rawResponse.Trim();
        if (cleanedResponse.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleanedResponse = cleanedResponse.Substring(7).TrimStart();
        }
        else if (cleanedResponse.StartsWith("```"))
        {
            var firstNewline = cleanedResponse.IndexOf('\n');
            if (firstNewline > -1)
            {
                cleanedResponse = cleanedResponse[(firstNewline + 1)..];
            }
        }

        if (cleanedResponse.EndsWith("```"))
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