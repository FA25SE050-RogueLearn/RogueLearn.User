// src/RogueLearn.User.Application/Plugins/QuestGenerationPlugin.cs
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using System.Text;
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
        WeekContext weekContext,
        string userContext,
        List<Skill> relevantSkills,
        string subjectName,
        string courseDescription,
        CancellationToken cancellationToken = default)
    {
        int maxAttempts = 3;
        string? lastError = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var prompt = _promptBuilder.BuildPrompt(
                weekContext,
                userContext,
                relevantSkills,
                subjectName,
                courseDescription,
                errorHint: lastError);

            try
            {
                _logger.LogInformation(
                    "Generating quest steps for Subject '{SubjectName}', Week {Week}/{Total} (Attempt {Attempt}/{Max})",
                    subjectName, weekContext.WeekNumber, weekContext.TotalWeeks, attempt, maxAttempts);

                var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
                var rawResponse = result.GetValue<string>() ?? string.Empty;

                var preview = rawResponse.Length > 300 ? rawResponse.Substring(0, 300) + "..." : rawResponse;
                _logger.LogInformation(
                    "Quest Generation - Week {Week} raw AI response length: {Length}. Preview: {Preview}",
                    weekContext.WeekNumber, rawResponse.Length, preview);

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
            subjectName, weekContext.WeekNumber, maxAttempts);
        return null;
    }

    /// <summary>
    /// Cleans the AI response to extract valid JSON for the weekly activity format.
    /// Expected output format: { "activities": [...] }
    /// </summary>
    private static string CleanToJson(string rawResponse)
    {
        var cleaned = rawResponse.Trim();

        // 1. Remove markdown code fences
        if (cleaned.StartsWith("````"))
        {
            var firstNewline = cleaned.IndexOf('\n');
            if (firstNewline > -1) cleaned = cleaned[(firstNewline + 1)..];
        }
        else if (cleaned.StartsWith("```"))
        {
            var firstNewline = cleaned.IndexOf('\n');
            if (firstNewline > -1) cleaned = cleaned[(firstNewline + 1)..];
        }

        if (cleaned.EndsWith("````"))
        {
            var lastFenceIndex = cleaned.LastIndexOf("````", StringComparison.Ordinal);
            if (lastFenceIndex > -1) cleaned = cleaned[..lastFenceIndex];
        }
        else if (cleaned.EndsWith("```"))
        {
            var lastFenceIndex = cleaned.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFenceIndex > -1) cleaned = cleaned[..lastFenceIndex];
        }

        cleaned = cleaned.Trim();

        // 2. Sanitize problematic characters
        // Replace literal newlines with space to prevent "0x0A is invalid" errors in JSON strings
        cleaned = cleaned.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

        // 3. Aggressively fix invalid escape sequences often produced by AI in LaTeX math
        // This looks for a backslash followed by a character that IS NOT a valid JSON escape char.
        // Valid JSON escapes: " \ / b f n r t u
        // Pattern: \ followed by NOT ( " or \ or / or b or f or n or r or t or u )
        // We double escape it (e.g., \int -> \\int) to treat it as a literal string.
        cleaned = Regex.Replace(cleaned, @"\\(?![""\\/bfnrtu])", @"\\\\");

        // 4. Fix trailing commas in arrays/objects (common AI error)
        cleaned = Regex.Replace(cleaned, @",\s*([\]}])", "$1");

        try
        {
            // 5. Validate JSON structure
            using var doc = JsonDocument.Parse(cleaned);
            if (!doc.RootElement.TryGetProperty("activities", out var activitiesElement)
                || activitiesElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Cleaned JSON does not contain a root 'activities' array.");
            }
            return cleaned;
        }
        catch (JsonException ex)
        {
            // 6. Fallback: Attempt to extract just the array if the root object wrapper is broken
            var arrStart = cleaned.IndexOf('[');
            var arrEnd = cleaned.LastIndexOf(']');
            if (arrStart >= 0 && arrEnd > arrStart)
            {
                var arrayJson = cleaned.Substring(arrStart, arrEnd - arrStart + 1);
                var reconstructed = "{\"activities\": " + arrayJson + "}";

                try
                {
                    using var doc = JsonDocument.Parse(reconstructed);
                    var activities = doc.RootElement.GetProperty("activities");
                    if (activities.ValueKind == JsonValueKind.Array)
                    {
                        return reconstructed;
                    }
                }
                catch
                {
                    // If reconstruction fails, throw original error
                }
            }

            throw new InvalidOperationException($"Cleaned response is not valid JSON. Error at Path: {ex.Path} | Position: {ex.BytePositionInLine}. Content snippet: {cleaned.Substring(0, Math.Min(100, cleaned.Length))}...", ex);
        }
    }
}
