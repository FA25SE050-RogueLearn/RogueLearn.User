// RogueLearn.User/src/RogueLearn.User.Application/Plugins/QuestGenerationPlugin.cs
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace RogueLearn.User.Application.Plugins;

public class QuestGenerationPlugin : IQuestGenerationPlugin
{
    private readonly Kernel _kernel;
    private readonly ILogger<QuestGenerationPlugin> _logger;

    public QuestGenerationPlugin(Kernel kernel, ILogger<QuestGenerationPlugin> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    // MODIFIED: The return type is now nullable to better signal a failure.
    public async Task<string?> GenerateQuestStepsJsonAsync(string syllabusJson, string userContext, CancellationToken cancellationToken = default)
    {
        var prompt = $@"
Analyze the following syllabus content and generate a series of interactive, gamified quest steps for a learning application.

**USER CONTEXT:**
{userContext}

**SYLLABUS CONTENT (JSON):**
{syllabusJson}

**TASK:**
Generate a JSON array of objects representing the quest steps. Follow these rules:
1.  Create between 5 and 8 steps to cover the main topics.
2.  Vary the 'stepType'. Use 'Reading' for introductions, 'Quiz' for knowledge checks, and 'Coding' or 'Interactive' for practical application.
3.  The content for each step should be engaging and directly related to the syllabus and user context. For 'Quiz' steps, include a 'questions' array in the content. For 'Coding', include a 'template' or 'challenge' string.
4.  Ensure the steps follow a logical learning progression.

**REQUIRED OUTPUT FORMAT:**
Return ONLY a JSON array of objects with this exact schema:
[
  {{
    ""stepNumber"": number,
    ""title"": ""string"",
    ""description"": ""string"",
    ""stepType"": ""string (MUST be one of these exact, case-sensitive values: 'Reading', 'Video', 'Interactive', 'Coding', 'Quiz', 'Discussion', 'Submission', 'Reflection')"",
    ""content"": {{ ""key"": ""value"" }}
  }}
]

Return only the JSON array, with no additional text or markdown formatting.
";

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
            // MODIFIED: Return null to signal a hard failure in the AI call.
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