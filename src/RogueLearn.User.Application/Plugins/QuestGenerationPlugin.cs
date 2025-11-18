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

    public async Task<string?> GenerateQuestStepsJsonAsync(
        string syllabusJson,
        string userContext,
        List<Skill> relevantSkills,
        string subjectName,
        string courseDescription,
        CancellationToken cancellationToken = default)
    {
        var prompt = _promptBuilder.BuildPrompt(
            syllabusJson, userContext, relevantSkills, subjectName, courseDescription);

        try
        {
            _logger.LogInformation("Generating quest steps for subject: {SubjectName}", subjectName);

            var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var rawResponse = result.GetValue<string>() ?? string.Empty;

            _logger.LogInformation("Quest Step Generation raw AI response: {RawResponse}", rawResponse);

            return CleanToJson(rawResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate quest steps using AI for subject {SubjectName}.", subjectName);
            return null;
        }
    }

    /// <summary>
    /// Cleans the AI response to extract valid JSON for the weekly module format.
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

        // CRITICAL FIX: Look for the OBJECT braces, not array brackets
        // We expect: { "activities": [...] }
        var startIdx = cleaned.IndexOf('{');
        var endIdx = cleaned.LastIndexOf('}');

        if (startIdx >= 0 && endIdx > startIdx)
        {
            cleaned = cleaned.Substring(startIdx, endIdx - startIdx + 1);
        }

        // Validate that we have a proper JSON object
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