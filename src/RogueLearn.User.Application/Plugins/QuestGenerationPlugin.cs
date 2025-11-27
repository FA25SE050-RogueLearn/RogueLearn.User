// RogueLearn.User/src/RogueLearn.User.Application/Plugins/QuestGenerationPlugin.cs
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RogueLearn.User.Application.Plugins;

public class QuestGenerationPlugin : IQuestGenerationPlugin
{
    private readonly Kernel _kernel;
    private readonly ILogger<QuestGenerationPlugin> _logger;
    private readonly QuestStepsPromptBuilder _promptBuilder;

    public QuestGenerationPlugin(
        Kernel kernel,
        ILogger<QuestGenerationPlugin> logger,
        QuestStepsPromptBuilder promptBuilder)
    {
        _kernel = kernel;
        _logger = logger;
        _promptBuilder = promptBuilder;
    }

    /// <summary>
    /// Generates quest steps for a SINGLE week using AI.
    /// This method is called multiple times (once per week) to generate all quest steps.
    /// </summary>
    public async Task<string?> GenerateQuestStepsJsonAsync(
        string syllabusJson,
        string userContext,
        List<Skill> relevantSkills,
        string subjectName,
        string courseDescription,
        int weekNumber,
        int totalWeeks,
        CancellationToken cancellationToken = default)
    {
        int maxAttempts = 3;
        string? lastError = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var prompt = _promptBuilder.BuildPrompt(
                syllabusJson,
                userContext,
                relevantSkills,
                subjectName,
                courseDescription,
                weekNumber,
                totalWeeks,
                errorHint: lastError);

            try
            {
                _logger.LogInformation(
                    "Generating quest steps for Subject '{SubjectName}', Week {Week}/{Total} (Attempt {Attempt}/{Max})",
                    subjectName, weekNumber, totalWeeks, attempt, maxAttempts);

                var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
                var rawResponse = result.GetValue<string>() ?? string.Empty;

                _logger.LogInformation(
                    "Quest Generation - Week {Week} raw AI response: {RawResponse}",
                    weekNumber, rawResponse);

                var cleaned = CleanToJson(rawResponse);
                return cleaned;
            }
            catch (InvalidOperationException ex)
            {
                lastError = ex.Message;
                _logger.LogWarning(
                    "Attempt {Attempt}/{Max} failed to clean/parse JSON: {Error}",
                    attempt, maxAttempts, ex.Message);
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                _logger.LogWarning(
                    ex,
                    "Attempt {Attempt}/{Max} failed due to exception.",
                    attempt, maxAttempts);
            }
        }

        _logger.LogError(
            "Failed to generate quest steps using AI for subject '{SubjectName}', Week {Week} after {Max} attempts.",
            subjectName, weekNumber, maxAttempts);
        return null;
    }

    /// <summary>
    /// Cleans the AI response to extract valid JSON for the weekly activity format.
    /// Expected output format: { "activities": [...] }
    /// </summary>
    private static string CleanToJson(string rawResponse)
    {
        var cleaned = rawResponse.Trim();

        // Remove markdown code fences
        if (cleaned.StartsWith("````") || cleaned.StartsWith("```"))
        {
            var firstNewline = cleaned.IndexOf('\n');
            if (firstNewline > -1)
            {
                cleaned = cleaned[(firstNewline + 1)..];
            }
        }

        if (cleaned.EndsWith("````") || cleaned.EndsWith("```"))
        {
            var lastFenceIndex = cleaned.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFenceIndex > -1)
            {
                cleaned = cleaned[..lastFenceIndex];
            }
        }

        cleaned = cleaned.Replace("\r", string.Empty).Trim();

        // Extract JSON object
        int idx = cleaned.IndexOf('{');
        if (idx >= 0)
        {
            int depth = 0;
            bool inString = false;
            bool escape = false;
            int end = -1;

            for (int i = idx; i < cleaned.Length; i++)
            {
                char c = cleaned[i];

                if (inString)
                {
                    if (escape)
                    {
                        escape = false;
                    }
                    else if (c == '\\')
                    {
                        escape = true;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inString = true;
                    }
                    else if (c == '{')
                    {
                        depth++;
                    }
                    else if (c == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            end = i;
                            break;
                        }
                    }
                }
            }

            if (end > idx)
            {
                cleaned = cleaned.Substring(idx, end - idx + 1);
            }
        }

        cleaned = cleaned.Trim();

        // ⭐ FIX: Escape invalid backslashes BEFORE parsing
        cleaned = Regex.Replace(cleaned, @"\\(?![""/\\bfnrtu])", @"\\\\");
        cleaned = Regex.Replace(cleaned, @",(\s*[\]}])", "$1");
        cleaned = Regex.Replace(cleaned, @"(?<!\\)\\[A-Za-z]+", m => "\\" + m.Value);
        cleaned = cleaned
            .Replace("\\(", "\\\\(")
            .Replace("\\)", "\\\\)")
            .Replace("\\[", "\\\\[")
            .Replace("\\]", "\\\\]")
            .Replace("\\{", "\\\\{")
            .Replace("\\}", "\\\\}");

        try
        {
            // Now parse - invalid escapes are already fixed
            using var doc = JsonDocument.Parse(cleaned);
            if (!doc.RootElement.TryGetProperty("activities", out var activitiesElement)
                || activitiesElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Cleaned JSON does not contain a root 'activities' array.");
            }
            return cleaned; // ⭐ Return the fixed version
        }
        catch (JsonException)
        {
            // Fallback: try to reconstruct from array
            var arrStart = cleaned.IndexOf('[');
            var arrEnd = cleaned.LastIndexOf(']');
            if (arrStart >= 0 && arrEnd > arrStart)
            {
                var arrayJson = cleaned.Substring(arrStart, arrEnd - arrStart + 1);
                var reconstructed = "{\"activities\": " + arrayJson + "}";
                reconstructed = Regex.Replace(reconstructed, @"\\(?![""/\\bfnrtu])", @"\\\\");
                reconstructed = Regex.Replace(reconstructed, @",(\s*[\]}])", "$1");
                reconstructed = Regex.Replace(reconstructed, @"(?<!\\)\\[A-Za-z]+", m => "\\" + m.Value);
                reconstructed = reconstructed
                    .Replace("\\(", "\\\\(")
                    .Replace("\\)", "\\\\)")
                    .Replace("\\[", "\\\\[")
                    .Replace("\\]", "\\\\]")
                    .Replace("\\{", "\\\\{")
                    .Replace("\\}", "\\\\}");
                // No need to escape again - already done above
                using var doc = JsonDocument.Parse(reconstructed);
                var activities = doc.RootElement.GetProperty("activities");
                if (activities.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException("Failed to reconstruct activities array from response.");
                }
                return reconstructed;
            }

            throw new InvalidOperationException($"Cleaned response is not valid JSON. Content: {cleaned}");
        }
    }
}
