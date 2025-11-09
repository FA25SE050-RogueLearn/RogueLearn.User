// RogueLearn.User/src/RogueLearn.User.Application/Plugins/QuestGenerationPlugin.cs
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using RogueLearn.User.Domain.Entities;
using System.Text.Json;

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

    // MODIFICATION: The method now accepts a list of Skill entities to ensure AI only uses valid, admin-approved skills.
    public async Task<string?> GenerateQuestStepsJsonAsync(string syllabusJson, string userContext, List<Skill> relevantSkills, CancellationToken cancellationToken = default)
    {
        // MODIFICATION: The prompt is dynamically updated with the list of pre-approved skills.
        // This provides the AI with a strict list of valid skillId-skillName pairs, preventing hallucination
        // and fulfilling the "Implementation" stage of the new architecture.
        var skillsJsonForPrompt = JsonSerializer.Serialize(
            relevantSkills.Select(s => new { skillId = s.Id, skillName = s.Name })
        );

        var prompt = $@"
Analyze the following syllabus content and generate a series of interactive, gamified quest steps for a learning application.

**USER CONTEXT:**
{userContext}

**PRE-APPROVED SKILLS (Use these ONLY):**
{skillsJsonForPrompt}

**SYLLABUS CONTENT (JSON):**
{syllabusJson}

**TASK:**
Generate a JSON array of 5 to 8 quest step objects. You MUST adhere to the following rules:

1.  Each object in the array MUST have the properties: `stepNumber`, `title`, `description`, `stepType`, `experiencePoints`, and `content`.
2.  The `stepType` property MUST be one of these exact, case-sensitive strings: 'Reading', 'Interactive', 'Quiz', 'Coding', 'Submission', 'Reflection'.
3.  The `experiencePoints` property MUST be an integer between 10 and 50, based on the step's complexity.
4.  The `content` object for each `stepType` MUST follow the specific schema defined below AND MUST include a `skillId`.

**CONTENT SCHEMAS (MANDATORY):**

- If `stepType` is **'Reading'**:
  `""content"": {{ ""skillId"": ""guid"", ""articleTitle"": ""string"", ""summary"": ""string"", ""url"": ""string"" }}`

- If `stepType` is **'Interactive'**:
  `""content"": {{ ""skillId"": ""guid"", ""challenge"": ""string"", ""questions"": [{{ ""task"": ""string"", ""options"": [""string""], ""answer"": ""string"" }}] }}`

- If `stepType` is **'Quiz'**:
  `""content"": {{ ""skillId"": ""guid"", ""questions"": [{{ ""question"": ""string"", ""options"": [""string""], ""correctAnswer"": ""string"", ""explanation"": ""string"" }}] }}`

- If `stepType` is **'Coding'**: (You will choose the suitable code problems provided from the given context)
  `""content"": {{ ""skillId"": ""guid"", ""challenge"": ""string"", ""template"": ""string (starter code)"", ""expectedOutput"": ""string"" }}`

- If `stepType` is **'Submission'**:
  `""content"": {{ ""skillId"": ""guid"", ""challenge"": ""string"", ""submissionFormat"": ""string"" }}`

- If `stepType` is **'Reflection'**:
  `""content"": {{ ""skillId"": ""guid"", ""challenge"": ""string"", ""reflectionPrompt"": ""string"", ""expectedOutcome"": ""string"" }}`

**CRITICAL RULE FOR `skillId`:**
The `skillId` value MUST be the UUID of the single, most relevant skill from the PRE-APPROVED SKILLS list that this specific step teaches. The `skillName` in that list is for your context only.

**OUTPUT REQUIREMENT:**
Return ONLY the raw JSON array. Do NOT wrap it in markdown backticks or add any other text.
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