// src/RogueLearn.User.Application/Plugins/QuestGenerationPlugin.cs
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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

    /// <summary>
    /// Generates quest steps for a SINGLE week using AI.
    /// This method is called multiple times (once per week) to generate all quest steps.
    /// </summary>
public async Task<string?> GenerateQuestStepsJsonAsync(
    WeekContext weekContext,
    string userContext,
    List<Skill> relevantSkills,
    string subjectName,
    string courseDescription,
    AcademicContext academicContext,
    Class? userClass = null,
    CancellationToken cancellationToken = default)
{
    const int maxAttempts = 3;
    string? lastError = null;

    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            _logger.LogInformation(
                "Generating quest steps for Subject '{Subject}', Week {Week}/{Total} (Attempt {Attempt}/{Max})",
                subjectName, weekContext.WeekNumber, weekContext.TotalWeeks, attempt, maxAttempts
            );

            // Log roadmap context if available
            if (userClass != null)
            {
                _logger.LogInformation(
                    "Roadmap Context: Track={TrackName}, RoadmapUrl={Url}, SkillFocus=[{Skills}], Difficulty={Difficulty}",
                    userClass.Name,
                    userClass.RoadmapUrl ?? "N/A",
                    userClass.SkillFocusAreas != null ? string.Join(", ", userClass.SkillFocusAreas) : "N/A",
                    userClass.DifficultyLevel);
            }

            // Build prompt with error feedback on retries
            var errorHint = attempt > 1 ? BuildRetryErrorHint(lastError) : null;

            var gpaBucket = academicContext.CurrentGpa >= 8.5 ? "High" : academicContext.CurrentGpa >= 7.0 ? "Good" : academicContext.CurrentGpa > 0 ? "Support" : "Unknown";
            var strengthsPreview = string.Join(", ", academicContext.StrengthAreas.Take(3));
            var improvementsPreview = string.Join(", ", academicContext.ImprovementAreas.Take(3));
            var weakPrereqCount = academicContext.PrerequisiteHistory.Count(p => p.PerformanceLevel == "Weak");
            _logger.LogInformation(
                "Personalization Context: GPA={Gpa:F2} ({Bucket}), Attempt={Reason}, WeakPrereqs={WeakCount}, Strengths=[{Strengths}], Improvements=[{Improvements}]",
                academicContext.CurrentGpa,
                gpaBucket,
                academicContext.AttemptReason,
                weakPrereqCount,
                strengthsPreview,
                improvementsPreview);

            var prompt = _promptBuilder.BuildPrompt(
                weekContext,
                userContext,
                relevantSkills,
                subjectName,
                courseDescription,
                academicContext,
                userClass: userClass,
                errorHint: errorHint
            );

            // Call LLM
            var rawResponse = await _kernel.InvokePromptAsync(
                prompt,
                cancellationToken: cancellationToken
            );

            var rawJson = rawResponse.ToString();

            _logger.LogInformation(
                "Quest Generation - Week {Week} raw AI response length: {Length}",
                weekContext.WeekNumber,
                rawJson.Length
            );

            // **USE THE CLEANER PIPELINE**
            var (success, cleanedJson, error) = EscapeSequenceCleaner.CleanAndValidate(rawJson);

            if (!success)
            {
                _logger.LogError(
                    "Attempt {Attempt}/{Max} failed to clean/parse JSON: {Error}",
                    attempt, maxAttempts, error
                );

                lastError = error;

                // Log problematic content on final attempt
                if (attempt == maxAttempts)
                {
                    var preview = rawJson.Length > 1000
                        ? rawJson.Substring(0, 1000) + "...[truncated]"
                        : rawJson;

                    _logger.LogError(
                        "JSON cleaning failure - Week {Week} - content preview:\n{Content}",
                        weekContext.WeekNumber,
                        preview
                    );
                }

                continue; // Retry
            }

            // Additional validation
            var (isValid, validationIssues) = EscapeSequenceCleaner.ValidateEscapeSequences(cleanedJson!);

            if (!isValid)
            {
                _logger.LogWarning(
                    "Week {Week} - Validation warnings: {Issues}",
                    weekContext.WeekNumber,
                    string.Join(", ", validationIssues)
                );
                // Continue anyway - warnings are non-fatal
            }

            _logger.LogInformation(
                "✅ Week {Week} - Successfully generated and validated JSON",
                weekContext.WeekNumber
            );

            return cleanedJson!;
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Attempt {Attempt}/{Max} - JSON parsing error for Week {Week}: {Error}",
                attempt, maxAttempts, weekContext.WeekNumber, ex.Message
            );

            lastError = $"JSON parsing failed at line {ex.LineNumber}: {ex.Message}";

            if (attempt == maxAttempts)
            {
                throw new InvalidOperationException(
                    $"Failed to generate valid JSON after {maxAttempts} attempts for Week {weekContext.WeekNumber}. " +
                    $"Last error: {lastError}",
                    ex
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Attempt {Attempt}/{Max} - Unexpected error for Week {Week}",
                attempt, maxAttempts, weekContext.WeekNumber
            );

            lastError = ex.Message;

            if (attempt == maxAttempts)
            {
                throw new InvalidOperationException(
                    $"Failed to generate quest steps after {maxAttempts} attempts for Week {weekContext.WeekNumber}",
                    ex
                );
            }
        }
    }

    throw new InvalidOperationException(
        $"Failed to generate quest steps after {maxAttempts} attempts for Week {weekContext.WeekNumber}. " +
        $"Last error: {lastError}"
    );
}

/// <summary>
/// Builds an error hint for retry attempts
/// </summary>
private string BuildRetryErrorHint(string? lastError)
{
    if (string.IsNullOrWhiteSpace(lastError))
    {
        return string.Empty;
    }

    return $@"
**⚠️ PREVIOUS ATTEMPT FAILED - PLEASE READ CAREFULLY**

Your last output had this error: {lastError}

**CRITICAL: How to handle C escape sequences in JSON**

When discussing C programming concepts like escape sequences, you MUST NOT use literal backslashes.

❌ WRONG - These will break JSON parsing:
  ""question"": ""What does \n represent?""
  ""explanation"": ""The \0 character marks the end""
  ""correctAnswer"": ""\t""

✅ CORRECT - Use descriptive text instead:
  ""question"": ""What does the newline escape sequence represent?""
  ""explanation"": ""The null character marks the end""
  ""correctAnswer"": ""tab character""

**Reference Guide:**
- Instead of \n → write ""newline"" or ""backslash-n""
- Instead of \t → write ""tab"" or ""backslash-t""
- Instead of \0 → write ""null character"" or ""backslash-zero""
- Instead of \r → write ""carriage return"" or ""backslash-r""
- Instead of \\ → write ""backslash"" or ""single backslash""

**OUTPUT REQUIREMENTS:**
1. Pure JSON only - no markdown code fences
2. No literal backslashes in string content
3. Describe escape sequences using plain English

Please regenerate the JSON following these rules strictly.
";
}
/// <summary>
/// Cleans the AI response to extract valid JSON for the weekly activity format.
/// Expected output format: { "activities": [...] }
/// </summary>
private static string CleanToJson(string rawResponse)
        {
            var cleaned = rawResponse.Trim();

        // 1. Remove markdown code fences
        if (cleaned.StartsWith("````"))
        {
            var firstNewline = cleaned.IndexOf('\n');
            if (firstNewline > -1) cleaned = cleaned[(firstNewline + 1)..];
        }
        else if (cleaned.StartsWith("```"))
        {
            var firstNewline = cleaned.IndexOf('\n');
            if (firstNewline > -1) cleaned = cleaned[(firstNewline + 1)..];
        }

        if (cleaned.EndsWith("````"))
        {
            var lastFenceIndex = cleaned.LastIndexOf("````", StringComparison.Ordinal);
            if (lastFenceIndex > -1) cleaned = cleaned[..lastFenceIndex];
        }
        else if (cleaned.EndsWith("```"))
        {
            var lastFenceIndex = cleaned.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFenceIndex > -1) cleaned = cleaned[..lastFenceIndex];
        }

        cleaned = cleaned.Trim();

        // 2. Sanitize problematic characters
        // Replace literal newlines with space to prevent "0x0A is invalid" errors in JSON strings
        cleaned = cleaned.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

        // 3. Aggressively fix invalid escape sequences often produced by AI in LaTeX math
        // This looks for a backslash followed by a character that IS NOT a valid JSON escape char.
        // Valid JSON escapes: " \ / b f n r t u
        // Pattern: \ followed by NOT ( " or \ or / or b or f or n or r or t or u )
        // We double escape it (e.g., \int -> \\int) to treat it as a literal string.
        cleaned = Regex.Replace(cleaned, @"\\(?![""\\/bfnrtu])", @"\\\\");

        // 4. Fix trailing commas in arrays/objects (common AI error)
        cleaned = Regex.Replace(cleaned, @",\s*([\]}])", "$1");

        try
        {
            // 5. Validate JSON structure
            cleaned = Regex.Replace(cleaned, @"\\0", @"\\\\0");
            cleaned = Regex.Replace(cleaned, @"\\\\\\0", @"\\\\0");
            cleaned = Regex.Replace(cleaned, @"\\a", @"\\\\a");
            cleaned = Regex.Replace(cleaned, @"\\v", @"\\\\v");
            using var doc = JsonDocument.Parse(cleaned);
            if (!doc.RootElement.TryGetProperty("activities", out var activitiesElement)
                || activitiesElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Cleaned JSON does not contain a root 'activities' array.");
            }
            return cleaned;
        }
        catch (JsonException ex)
        {
            // 6. Fallback: Attempt to extract just the array if the root object wrapper is broken
            var arrStart = cleaned.IndexOf('[');
            var arrEnd = cleaned.LastIndexOf(']');
            if (arrStart >= 0 && arrEnd > arrStart)
            {
                var arrayJson = cleaned.Substring(arrStart, arrEnd - arrStart + 1);
                var reconstructed = "{\"activities\": " + arrayJson + "}";

                try
                {
                    using var doc = JsonDocument.Parse(reconstructed);
                    var activities = doc.RootElement.GetProperty("activities");
                    if (activities.ValueKind == JsonValueKind.Array)
                    {
                        return reconstructed;
                    }
                }
                catch
                {
                    throw new CleaningJsonException(
                        "Reconstruction failed after parse error.",
                        reconstructed,
                        ex);
                }
            }

            throw new CleaningJsonException(
                $"Cleaned response is not valid JSON. Path: {ex.Path} | Position: {ex.BytePositionInLine}",
                cleaned,
                ex);
        }
    }
}
    internal class CleaningJsonException : InvalidOperationException
    {
        public string CleanedContent { get; }
        public CleaningJsonException(string message, string cleanedContent, Exception inner)
            : base(message, inner)
        {
            CleanedContent = cleanedContent;
        }
    }
