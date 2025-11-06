// RogueLearn.User/src/RogueLearn.User.Application/Plugins/SkillDependencyAnalysisPlugin.cs
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace RogueLearn.User.Application.Plugins;

public class SkillDependencyAnalysisPlugin : ISkillDependencyAnalysisPlugin
{
    private readonly Kernel _kernel;
    private readonly ILogger<SkillDependencyAnalysisPlugin> _logger;

    public SkillDependencyAnalysisPlugin(Kernel kernel, ILogger<SkillDependencyAnalysisPlugin> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<List<SkillDependencyAnalysis>> AnalyzeSkillDependenciesAsync(List<string> skillNames, CancellationToken cancellationToken)
    {
        var skillList = string.Join("\n", skillNames.Select(s => $"- {s}"));

        var prompt = $@"
Analyze the following list of skills from a single university subject. Determine the prerequisite relationships between them.

SKILL LIST:
{skillList}

TASK:
Identify which skills are prerequisites for other skills WITHIN THIS LIST.
Return a JSON array of objects with this exact schema:
[
  {{
    ""skillName"": ""string (the more advanced skill)"",
    ""prerequisiteSkillName"": ""string (the skill that should be learned first)"",
    ""reasoning"": ""string (a brief explanation of the dependency)""
  }}
]

RULES:
- A skill can have multiple prerequisites.
- Only create dependencies between skills in the provided list.
- If no dependencies are found, return an empty array [].
- Return ONLY the raw JSON array. Do NOT wrap it in markdown.

JSON RESPONSE:
";
        try
        {
            var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var jsonResponse = CleanToJson(result.GetValue<string>() ?? "[]");

            _logger.LogInformation("AI Skill Dependency Analysis Raw Response: {JsonResponse}", jsonResponse);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<SkillDependencyAnalysis>>(jsonResponse, options) ?? new List<SkillDependencyAnalysis>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze skill dependencies with AI.");
            return new List<SkillDependencyAnalysis>();
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