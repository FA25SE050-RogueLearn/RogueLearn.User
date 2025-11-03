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

    public async Task<string?> GenerateQuestStepsJsonAsync(string syllabusJson, string userContext, CancellationToken cancellationToken = default)
    {
        // MODIFIED: The prompt now includes instructions for generating experiencePoints for each step.
        var prompt = $@"
Analyze the following syllabus content and generate a series of interactive, gamified quest steps for a learning application.

**USER CONTEXT:**
{userContext}

**SYLLABUS CONTENT (JSON):**
{syllabusJson}

**TASK:**
Generate a JSON array of 5 to 8 quest step objects. You MUST adhere to the following rules:

1.  Each object in the array MUST have the properties: `stepNumber`, `title`, `description`, `stepType`, `experiencePoints`, and `content`.
2.  The `stepType` property MUST be one of these exact, case-sensitive strings: 'Reading', 'Interactive', 'Quiz', 'Coding', 'Submission', 'Reflection'.
3.  The `experiencePoints` property MUST be an integer between 10 and 50, based on the step's complexity.
4.  The `content` object for each `stepType` MUST follow the specific schema defined below AND MUST include a `skillTag`.

**CONTENT SCHEMAS (MANDATORY):**

- If `stepType` is **'Reading'**:
  `""content"": {{ ""skillTag"": ""string"", ""articleTitle"": ""string"", ""summary"": ""string"", ""url"": ""string"" }}`

- If `stepType` is **'Interactive'**:
  `""content"": {{ ""skillTag"": ""string"", ""challenge"": ""string"", ""questions"": [{{ ""task"": ""string"", ""options"": [""string""], ""answer"": ""string"" }}] }}`

- If `stepType` is **'Quiz'**:
  `""content"": {{ ""skillTag"": ""string"", ""questions"": [{{ ""question"": ""string"", ""options"": [""string""], ""correctAnswer"": ""string"", ""explanation"": ""string"" }}] }}`

- If `stepType` is **'Coding'**: (This is a descriptive challenge, not an executable one)
  `""content"": {{ ""skillTag"": ""string"", ""challenge"": ""string"", ""template"": ""string (starter code)"", ""expectedOutput"": ""string"" }}`

- If `stepType` is **'Submission'**:
  `""content"": {{ ""skillTag"": ""string"", ""challenge"": ""string"", ""submissionFormat"": ""string"" }}`

- If `stepType` is **'Reflection'**:
  `""content"": {{ ""skillTag"": ""string"", ""challenge"": ""string"", ""reflectionPrompt"": ""string"", ""expectedOutcome"": ""string"" }}`

**CRITICAL RULE FOR `skillTag`:**
The `skillTag` value MUST be the name of the single, most relevant skill from the syllabus that this specific step teaches. Examples: 'Servlet Lifecycle Management', 'JSP Expression Language', 'MVC Pattern Implementation'.

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