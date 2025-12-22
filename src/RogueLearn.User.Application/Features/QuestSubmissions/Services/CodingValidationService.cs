using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.QuestSubmissions.Services;

public class CodingValidationService : ICodingValidationService
{
    private readonly Kernel _kernel;
    private readonly ILogger<CodingValidationService> _logger;

    public CodingValidationService(Kernel kernel, ILogger<CodingValidationService> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<(bool isPassed, int score, string feedback)> EvaluateCodeAsync(
        string studentCode,
        string language,
        string description,
        string? validationCriteria,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Submitting student code for AI grading. Language: {Language}", language);

        if (string.IsNullOrWhiteSpace(studentCode))
        {
            return (false, 0, "No code provided.");
        }

        var systemPrompt = @"You are an expert Code Reviewer and Auto-Grader. 
Your job is to evaluate student code submissions for correctness, logic, and style.
You do NOT execute the code, but you analyze it statically.

Output ONLY JSON in the following format:
{
    ""passed"": boolean, // True if logic is correct and meets criteria
    ""score"": number,   // 0-100
    ""feedback"": ""string"" // Constructive feedback, mention bugs or style improvements
}";

        var userPrompt = $@"
## Problem Description
{description}

## Validation Criteria (Hidden from Student)
{validationCriteria ?? "Standard code correctness."}

## Student Submission ({language})
```
{studentCode}
```

## Task
1. Analyze if the code solves the problem.
2. Check against the validation criteria.
3. Check for syntax errors or logical bugs.
4. Grade it (0-100). Passing is usually > 60.

Return the JSON result.";

        try
        {
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory(systemPrompt);
            history.AddUserMessage(userPrompt);

            var result = await chatService.GetChatMessageContentAsync(history, cancellationToken: cancellationToken);
            var jsonContent = CleanJson(result?.Content ?? "{}");

            var response = JsonSerializer.Deserialize<AiGradingResult>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (response == null) return (false, 0, "AI Grading failed to parse.");

            return (response.Passed, response.Score, response.Feedback ?? "No feedback provided.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Grading failed.");
            return (false, 0, "Grading service unavailable. Please try again.");
        }
    }

    private string CleanJson(string raw)
    {
        var cleaned = raw.Trim();
        if (cleaned.StartsWith("```json")) cleaned = cleaned.Substring(7);
        if (cleaned.StartsWith("```")) cleaned = cleaned.Substring(3);
        if (cleaned.EndsWith("```")) cleaned = cleaned.Substring(0, cleaned.Length - 3);
        return cleaned.Trim();
    }

    private class AiGradingResult
    {
        public bool Passed { get; set; }
        public int Score { get; set; }
        public string? Feedback { get; set; }
    }
}