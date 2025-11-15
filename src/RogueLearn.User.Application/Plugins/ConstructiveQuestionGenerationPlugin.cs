using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using RogueLearn.User.Application.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Plugins;

public class ConstructiveQuestionGenerationPlugin : IConstructiveQuestionGenerationPlugin
{
    private readonly Kernel _kernel;
    private readonly ILogger<ConstructiveQuestionGenerationPlugin> _logger;
    private readonly string _promptPath;

    public ConstructiveQuestionGenerationPlugin(Kernel kernel, ILogger<ConstructiveQuestionGenerationPlugin> logger)
    {
        _kernel = kernel;
        _logger = logger;
        _promptPath = Path.Combine(AppContext.BaseDirectory, "Features", "CurriculumImport", "Prompts", "GenerateConstructiveQuestionsPrompt.txt");
    }

    public async Task<List<ConstructiveQuestion>> GenerateQuestionsAsync(List<SyllabusSessionDto> sessionSchedule, CancellationToken cancellationToken = default)
    {
        if (!sessionSchedule.Any())
        {
            return new List<ConstructiveQuestion>();
        }

        try
        {
            if (!File.Exists(_promptPath))
            {
                _logger.LogError("Critical Error: Constructive Question Generation prompt file not found at {Path}", _promptPath);
                throw new FileNotFoundException("The question generation prompt template file is missing.", _promptPath);
            }

            var promptTemplate = await File.ReadAllTextAsync(_promptPath, cancellationToken);
            var scheduleJson = JsonSerializer.Serialize(sessionSchedule, new JsonSerializerOptions { WriteIndented = true });
            var prompt = promptTemplate.Replace("{{SESSION_SCHEDULE_JSON}}", scheduleJson);

            var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var rawResponse = result.GetValue<string>() ?? "[]";

            _logger.LogInformation("Constructive Question Generation raw AI response: {RawResponse}", rawResponse);

            var cleanedJson = CleanToJson(rawResponse);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
            var questions = JsonSerializer.Deserialize<List<ConstructiveQuestion>>(cleanedJson, options);

            return questions ?? new List<ConstructiveQuestion>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate constructive questions using AI.");
            return new List<ConstructiveQuestion>();
        }
    }

    private static string CleanToJson(string rawResponse)
    {
        var cleanedResponse = rawResponse.Trim();
        if (cleanedResponse.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleanedResponse = cleanedResponse.Substring(7).TrimStart();
        }
        else if (cleanedResponse.StartsWith("```"))
        {
            var firstNewline = cleanedResponse.IndexOf('\n');
            if (firstNewline > -1)
            {
                cleanedResponse = cleanedResponse[(firstNewline + 1)..];
            }
        }

        if (cleanedResponse.EndsWith("```"))
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