// RogueLearn.User/src/RogueLearn.User.Application/Plugins/SubjectExtractionPlugin.cs
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Threading;
using System.Threading.Tasks;

namespace RogueLearn.User.Application.Plugins;

/// <summary>
/// Implementation of ISubjectExtractionPlugin using Semantic Kernel to call a generative AI model.
/// This plugin is specifically tailored to extract details for a single subject.
/// </summary>
public class SubjectExtractionPlugin : ISubjectExtractionPlugin
{
    private readonly Kernel _kernel;
    private readonly ILogger<SubjectExtractionPlugin> _logger;

    public SubjectExtractionPlugin(Kernel kernel, ILogger<SubjectExtractionPlugin> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<string> ExtractSubjectJsonAsync(string rawSubjectText, CancellationToken cancellationToken = default)
    {
        var header = @"
Analyze the following text describing a single academic subject and extract its details.
Return ONLY a single JSON object following this exact schema:

{
  ""subjectCode"": ""string (e.g., 'VOV114')"",
  ""subjectName"": ""string (e.g., 'Vovinam 1')"",
  ""credits"": number,
  ""description"": ""string (a concise summary of the subject's objective, content, and assessment)""
}

Important Rules:
- Infer the values logically from the provided text.
- If a value cannot be found, use a reasonable default (null or an empty string for strings, 0 for numbers).
- Do NOT invent a program, version, or curriculum structure. Focus only on the subject details.
- Return ONLY the JSON object, with no additional commentary or markdown formatting.

Text to extract from:
";

        var prompt = header + rawSubjectText + @"

Return only the JSON object.";

        try
        {
            var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var rawResponse = result.GetValue<string>() ?? string.Empty;
            _logger.LogInformation("Single Subject extractor raw response: {RawResponse}", rawResponse);
            return CleanToJson(rawResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract single subject data using AI");
            return string.Empty;
        }
    }

    // ADDED: Implementation of the new method to extract a skill from a learning objective.
    public async Task<string> ExtractSkillFromObjectiveAsync(string learningObjectiveText, CancellationToken cancellationToken = default)
    {
        var prompt = $@"
From the following learning objective sentence, extract the single most important, concise skill or topic name.

RULES:
- The output MUST be 2-4 words maximum.
- The output should be a noun phrase representing the core concept.
- Examples:
  - Input: 'To master the contents regarding the emergence, stages of development, subject, methodology, and significance of studying Scientific Socialism.'
    Output: 'Scientific Socialism'
  - Input: 'Develop skills of argument, writing, presentations, critical thinking, handling social relations and group activities.'
    Output: 'Critical Thinking and Communication'
  - Input: 'Master the Marxist-Leninist views on socialist democracy and the socialist state...'
    Output: 'Socialist Democracy'

Return ONLY the skill name, with no other text.

Learning Objective:
""{learningObjectiveText}""

Extracted Skill Name:
";
        try
        {
            var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            return result.GetValue<string>()?.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract skill from learning objective using AI for text: '{ObjectiveText}'", learningObjectiveText);
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