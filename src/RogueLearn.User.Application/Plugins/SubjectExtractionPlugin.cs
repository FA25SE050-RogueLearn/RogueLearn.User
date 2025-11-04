// RogueLearn.User/src/RogueLearn.User/Application/Plugins/SubjectExtractionPlugin.cs
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text.Json;
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

        // MODIFIED: Corrected the variable name from 'rawText' to 'rawSubjectText' to match the method parameter.
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

    public async Task<List<string>> ExtractSkillsFromObjectivesAsync(List<string> learningObjectives, CancellationToken cancellationToken = default)
    {
        if (learningObjectives == null || !learningObjectives.Any())
        {
            return new List<string>();
        }

        var objectivesList = string.Join("\n", learningObjectives.Select((lo, i) => $"{i + 1}. {lo}"));

        var prompt = $@"
Analyze the following numbered list of learning objectives. For each objective, extract the single most important, concise skill or topic name.

RULES:
- Your response MUST be a single JSON object with a single key ""skills"", which is an array of strings.
- The array MUST contain exactly {learningObjectives.Count} strings.
- The Nth string in the array MUST correspond to the Nth learning objective in the input list.
- Each skill name MUST be 2-4 words maximum and represent a core noun phrase.
- If you cannot determine a skill for an objective, return an empty string for that position in the array.
- Do NOT include markdown fences or any text outside the single JSON object.

EXAMPLE INPUT:
1. To master the contents regarding the emergence, stages of development, subject, methodology, and significance of studying Scientific Socialism.
2. Develop skills of argument, writing, presentations, critical thinking, handling social relations and group activities.

EXAMPLE OUTPUT:
{{
  ""skills"": [
    ""Scientific Socialism"",
    ""Critical Thinking and Communication""
  ]
}}

Learning Objectives to process:
{objectivesList}

Return ONLY the JSON object:
";
        try
        {
            var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var jsonResponse = CleanToJson(result.GetValue<string>() ?? "{}");

            _logger.LogInformation("AI Batch Skill Extraction Raw Response: {JsonResponse}", jsonResponse);

            var skillResponse = JsonSerializer.Deserialize<SkillExtractionResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return skillResponse?.Skills ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract skills in batch from learning objectives using AI.");
            return Enumerable.Repeat(string.Empty, learningObjectives.Count).ToList();
        }
    }

    private class SkillExtractionResponse
    {
        public List<string> Skills { get; set; } = new();
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