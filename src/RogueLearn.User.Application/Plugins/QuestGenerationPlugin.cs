using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using RogueLearn.User.Application.Services;
using System.Text.Json;

namespace RogueLearn.User.Application.Plugins;

public class QuestGenerationPlugin : IQuestGenerationPlugin
{
    private readonly Kernel _kernel;
    private readonly ILogger<QuestGenerationPlugin> _logger;

    // Note: PromptBuilder dependency removed as we now inject the raw prompt string directly.

    public QuestGenerationPlugin(
        Kernel kernel,
        ILogger<QuestGenerationPlugin> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    /// <summary>
    /// Executes a prompt to generate a Master Quest Module with 3 difficulty variants.
    /// Validates that the output contains 'standard', 'supportive', and 'challenging' keys.
    /// </summary>
    public async Task<string?> GenerateFromPromptAsync(string prompt, CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 3;
        string? lastError = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                _logger.LogInformation("Executing Master Quest AI Prompt (Attempt {Attempt}/{Max})", attempt, maxAttempts);

                var finalPrompt = prompt;
                if (attempt > 1 && !string.IsNullOrWhiteSpace(lastError))
                {
                    finalPrompt += BuildRetryErrorHint(lastError);
                }

                var rawResponse = await _kernel.InvokePromptAsync(finalPrompt, cancellationToken: cancellationToken);
                var rawJson = rawResponse.ToString();

                _logger.LogInformation("AI Response received. Length: {Length}", rawJson.Length);

                var (success, cleanedJson, error) = EscapeSequenceCleaner.CleanAndValidate(rawJson);

                if (!success)
                {
                    _logger.LogError("JSON cleaning failed: {Error}", error);
                    // Log first 500 chars of the raw response to debug the format
                    var preview = rawJson.Length > 500 ? rawJson.Substring(0, 500) + "..." : rawJson;
                    _logger.LogError("Raw AI Response Preview: {Preview}", preview);

                    lastError = error;
                    continue;
                }

                // Validate Structure: Must have 3 keys for the parallel tracks
                try
                {
                    using var doc = JsonDocument.Parse(cleanedJson!);
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("standard", out _) ||
                        !root.TryGetProperty("supportive", out _) ||
                        !root.TryGetProperty("challenging", out _))
                    {
                        throw new JsonException("Missing required difficulty keys (standard, supportive, challenging). The AI must generate all 3 tracks.");
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError("Validation failed: {Message}", ex.Message);
                    lastError = ex.Message;
                    continue;
                }

                return cleanedJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected AI error during Master Quest generation");
                lastError = ex.Message;
            }
        }

        _logger.LogError("Failed to generate valid Master Quest JSON after {Max} attempts.", maxAttempts);
        return null;
    }

    /// <summary>
    /// Builds an error hint for retry attempts
    /// </summary>
    private string BuildRetryErrorHint(string? lastError)
    {
        if (string.IsNullOrWhiteSpace(lastError))
        {
            return string.Empty;
        }

        return $@"
**⚠️ PREVIOUS ATTEMPT FAILED - PLEASE READ CAREFULLY**

Your last output had this error: {lastError}

**CRITICAL CHECKLIST:**
1. Did you include ALL 3 keys: ""standard"", ""supportive"", ""challenging""?
2. Is the JSON syntax valid (commas, brackets, quotes)?
3. Did you avoid using literal backslashes for escape sequences? (Use 'newline' instead of \n)

Please regenerate the JSON following these rules strictly.
";
    }
}