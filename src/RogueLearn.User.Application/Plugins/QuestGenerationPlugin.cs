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

        // Remove markdown code fences if present
        if (cleaned.StartsWith("```"))
        {
            // Remove opening fence and language identifier (e.g., ```json)
            var firstNewline = cleaned.IndexOf('\n');
            if (firstNewline > -1)
            {
                cleaned = cleaned[(firstNewline + 1)..];
            }
        }

        if (cleaned.EndsWith("```"))
        {
            var lastFenceIndex = cleaned.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFenceIndex > -1)
            {
                cleaned = cleaned[..lastFenceIndex];
            }
        }

        cleaned = cleaned.Trim();

        // Extract JSON object: { "activities": [...] }
        var startIdx = cleaned.IndexOf('{');
        var endIdx = cleaned.LastIndexOf('}');

        if (startIdx >= 0 && endIdx > startIdx)
        {
            cleaned = cleaned.Substring(startIdx, endIdx - startIdx + 1);
        }

        // Validate JSON structure
        try
        {
            using var doc = JsonDocument.Parse(cleaned);
            if (!doc.RootElement.TryGetProperty("activities", out var activitiesElement) ||
                activitiesElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException(
                    "Cleaned JSON does not contain a root 'activities' array. " +
                    "This indicates the AI did not follow the prompt format.");
            }
        }
        catch (JsonException)
        {
            throw new InvalidOperationException(
                $"Cleaned response is not valid JSON. Content: {cleaned}");
        }

        return cleaned.Trim();
    }
}