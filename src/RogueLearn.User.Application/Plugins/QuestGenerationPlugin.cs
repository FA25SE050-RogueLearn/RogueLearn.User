// RogueLearn.User/src/RogueLearn.User.Application/Plugins/QuestGenerationPlugin.cs
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using System.Text.Json;

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
        // Build prompt for the specific week
        var prompt = _promptBuilder.BuildPrompt(
            syllabusJson,
            userContext,
            relevantSkills,
            subjectName,
            courseDescription,
            weekNumber,
            totalWeeks);

        try
        {
            _logger.LogInformation(
                "Generating quest steps for Subject '{SubjectName}', Week {Week}/{Total}",
                subjectName, weekNumber, totalWeeks);

            var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var rawResponse = result.GetValue<string>() ?? string.Empty;

            _logger.LogInformation(
                "Quest Generation - Week {Week} raw AI response: {RawResponse}",
                weekNumber, rawResponse);

            return CleanToJson(rawResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to generate quest steps using AI for subject '{SubjectName}', Week {Week}.",
                subjectName, weekNumber);
            return null;
        }
    }

    /// <summary>
    /// Cleans the AI response to extract valid JSON for the weekly activity format.
    /// Expected output format: { "activities": [...] }
    /// </summary>
    private static string CleanToJson(string rawResponse)
    {
        var cleaned = rawResponse.Trim();

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

        try
        {
            using var doc = JsonDocument.Parse(cleaned);
            if (!doc.RootElement.TryGetProperty("activities", out var activitiesElement) || activitiesElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Cleaned JSON does not contain a root 'activities' array.");
            }
        }
        catch (JsonException)
        {
            var arrStart = cleaned.IndexOf('[');
            var arrEnd = cleaned.LastIndexOf(']');
            if (arrStart >= 0 && arrEnd > arrStart)
            {
                var arrayJson = cleaned.Substring(arrStart, arrEnd - arrStart + 1);
                var reconstructed = "{\"activities\": " + arrayJson + "}";
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

        return cleaned;
    }
}
