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

    public QuestGenerationPlugin(Kernel kernel, ILogger<QuestGenerationPlugin> logger, QuestStepsPromptBuilder promptBuilder)
    {
        _kernel = kernel;
        _logger = logger;
        _promptBuilder = promptBuilder;
    }

    // MODIFICATION: The method now accepts a list of Skill entities to ensure AI only uses valid, admin-approved skills.
    public async Task<string?> GenerateQuestStepsJsonAsync(string syllabusJson, string userContext, List<Skill> relevantSkills, CancellationToken cancellationToken = default)
    {
        // Build a comprehensive, LLM-friendly prompt using the dedicated prompt builder
        var prompt = _promptBuilder.BuildPrompt(syllabusJson, userContext, relevantSkills);

        try
        {
            var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var rawResponse = result.GetValue<string>() ?? string.Empty;
            _logger.LogInformation("Quest Step Generation raw AI response: {RawResponse}", rawResponse);
            return CleanToJson(rawResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate quest steps using AI.");
            return null;
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
        var startIdx = cleanedResponse.IndexOf('[');
        var endIdx = cleanedResponse.LastIndexOf(']');
        if (startIdx >= 0 && endIdx > startIdx)
        {
            cleanedResponse = cleanedResponse.Substring(startIdx, endIdx - startIdx + 1);
        }
        return cleanedResponse.Trim();
    }
}