using System.Text.RegularExpressions;

namespace RogueLearn.User.Application.Services;

/// <summary>
/// Cleans problematic escape sequences from LLM-generated JSON content
/// </summary>
public static class EscapeSequenceCleaner
{
    /// <summary>
    /// Cleans escape sequences in JSON string values by converting them to readable text
    /// </summary>
    public static string CleanEscapeSequences(string json)
    {
        if (string.IsNullOrEmpty(json)) return json;

        // Use verbatim string (@"...") to avoid double-escaping hell
        // This regex finds all JSON string values: "anything here"
        return Regex.Replace(json, @"""([^""\\]*(\\.[^""\\]*)*)""", match =>
        {
            var value = match.Groups[1].Value;
            var cleaned = CleanStringValue(value);
            return $"\"{cleaned}\"";
        });
    }

    /// <summary>
    /// Cleans escape sequences within a single string value
    /// </summary>
    private static string CleanStringValue(string value)
    {
        // Use verbatim strings (@"...") for patterns to avoid confusion
        // In verbatim strings, only " needs escaping as ""
        var replacements = new Dictionary<string, string>
        {
            // 8 backslashes in source = 4 actual backslashes (e.g., \\\\0 in JSON)
            { @"\\\\0", "null character" },
            { @"\\\\n", "newline" },
            { @"\\\\t", "tab" },
            { @"\\\\r", "carriage return" },
            { @"\\\\f", "form feed" },
            { @"\\\\v", "vertical tab" },
            { @"\\\\", "backslash" },
            
            // 4 backslashes in source = 2 actual backslashes (e.g., \\0 in JSON)
            { @"\\0", "null character" },
            { @"\\n", "newline" },
            { @"\\t", "tab" },
            { @"\\r", "carriage return" },
            { @"\\f", "form feed" },
            { @"\\v", "vertical tab" },
            
            // Patterns in markdown code (with backticks)
            { "`\\n`", "the newline character" },
            { "`\\t`", "the tab character" },
            { "`\\0`", "the null character" },
            { "`\\r`", "the carriage return" },
            { "`\\a`", "the alert character" },
            { "`\\v`", "the vertical tab" },
            { "`\\\\`", "a backslash" },
            
            // Patterns in explanatory text
            { "The `\\n` escape", "The newline escape" },
            { "The `\\t` escape", "The tab escape" },
            { "The `\\0` character", "The null character" },
            { "The `\\r` escape", "The carriage return escape" },
            { "\\n escape", "newline escape" },
            { "\\t escape", "tab escape" },
            { "\\0 character", "null character" },
            { "\\r character", "carriage return" },
        };

        var result = value;

        // Apply replacements in order (more specific first)
        foreach (var kvp in replacements)
        {
            result = result.Replace(kvp.Key, kvp.Value);
        }

        // Safety: reduce any remaining runs of 3+ backslashes to 2
        result = Regex.Replace(result, @"\\{3,}", @"\\");

        return result;
    }

    /// <summary>
    /// Validates JSON for common escape sequence problems
    /// </summary>
    public static (bool isValid, List<string> issues) ValidateEscapeSequences(string json)
    {
        var issues = new List<string>();

        // Check for runs of 4+ backslashes (over-escaping)
        if (Regex.IsMatch(json, @"\\{4,}"))
        {
            issues.Add("Found over-escaped sequences (4+ backslashes in a row)");
        }

        // Check for specific problematic patterns
        var problematicPatterns = new[]
        {
            (pattern: @"\\\\\\\\0", description: "Octuple-escaped null character found"),
            (pattern: @"\\\\\\\\n", description: "Octuple-escaped newline found"),
            (pattern: @"[^\\]\\n[^n]", description: "Potentially incorrect newline escape"),
            (pattern: @"[^\\]\\t[^t]", description: "Potentially incorrect tab escape"),
            (pattern: @"[^\\]\\0[^0]", description: "Potentially incorrect null character"),
        };

        foreach (var (pattern, description) in problematicPatterns)
        {
            if (Regex.IsMatch(json, pattern))
            {
                issues.Add(description);
            }
        }

        return (issues.Count == 0, issues);
    }

    /// <summary>
    /// Aggressively normalizes escape sequences - use as last resort
    /// </summary>
    public static string NormalizeEscapeSequences(string json)
    {
        if (string.IsNullOrEmpty(json)) return json;

        // First, reduce excessive backslash runs before escape characters
        // Pattern: 4 or more backslashes followed by a known escape character
        json = Regex.Replace(json, @"\\{4,}([0ntrfvabe\\""'])", m =>
        {
            // Keep only 2 backslashes (which represents 1 backslash in the actual string)
            return @"\\" + m.Groups[1].Value;
        });

        // Then replace common escape sequence patterns with readable text
        var patterns = new Dictionary<string, string>
        {
            // Match backslashes followed by escape chars at end of strings or before punctuation
            { @"\\+0(?=[""\s,}])", "null character" },
            { @"\\+n(?=[""\s,}])", "newline" },
            { @"\\+t(?=[""\s,}])", "tab" },
            { @"\\+r(?=[""\s,}])", "carriage return" },
            { @"\\+f(?=[""\s,}])", "form feed" },
            { @"\\+v(?=[""\s,}])", "vertical tab" },
            { @"\\+\\(?=[""\s,}])", "backslash" }
        };

        foreach (var (pattern, replacement) in patterns)
        {
            json = Regex.Replace(json, pattern, replacement);
        }

        return json;
    }

    /// <summary>
    /// Complete cleaning pipeline - tries multiple strategies
    /// </summary>
    public static (bool success, string? cleanedJson, string? error) CleanAndValidate(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return (false, null, "Input JSON is empty");
        }

        // Remove markdown code fences if present
        rawJson = Regex.Replace(rawJson, @"^```json\s*", "", RegexOptions.Multiline);
        rawJson = Regex.Replace(rawJson, @"^```\s*$", "", RegexOptions.Multiline);
        rawJson = rawJson.Trim();

        // Strategy 1: Basic cleaning
        try
        {
            var cleaned = CleanEscapeSequences(rawJson);
            using var doc = System.Text.Json.JsonDocument.Parse(cleaned);

            // ⭐ UPDATED: Validate structure
            // Support BOTH old schema (root 'activities') AND new schema (root 'standard')
            var root = doc.RootElement;
            bool isOldSchema = root.TryGetProperty("activities", out _);
            bool isNewSchema = root.TryGetProperty("standard", out _) || root.TryGetProperty("Standard", out _);

            if (!isOldSchema && !isNewSchema)
            {
                return (false, null, "JSON root missing required keys (either 'activities' or 'standard')");
            }

            return (true, cleaned, null);
        }
        catch (System.Text.Json.JsonException)
        {
            // Strategy 2: Aggressive normalization
            try
            {
                var normalized = NormalizeEscapeSequences(rawJson);
                var cleaned = CleanEscapeSequences(normalized);

                using var doc = System.Text.Json.JsonDocument.Parse(cleaned);

                var root = doc.RootElement;
                bool isOldSchema = root.TryGetProperty("activities", out _);
                bool isNewSchema = root.TryGetProperty("standard", out _) || root.TryGetProperty("Standard", out _);

                if (!isOldSchema && !isNewSchema)
                {
                    return (false, null, "Missing required keys after normalization");
                }

                return (true, cleaned, null);
            }
            catch (System.Text.Json.JsonException ex2)
            {
                return (false, null, $"JSON parsing failed: {ex2.Message} (Line: {ex2.LineNumber}, Position: {ex2.BytePositionInLine})");
            }
            catch (Exception ex2)
            {
                return (false, null, $"Unexpected error during normalization: {ex2.Message}");
            }
        }
        catch (Exception ex)
        {
            return (false, null, $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Extracts context around a JSON parsing error for debugging
    /// </summary>
    public static string GetErrorContext(string json, System.Text.Json.JsonException ex)
    {
        try
        {
            var lineNumber = ex.LineNumber ?? 0;
            var lines = json.Split('\n');

            if (lineNumber > 0 && lineNumber <= lines.Length)
            {
                var startLine = Math.Max(0, (int)lineNumber - 3);
                var endLine = Math.Min(lines.Length, (int)lineNumber + 3);

                var contextLines = new List<string>();
                for (int i = startLine; i < endLine; i++)
                {
                    var marker = (i == lineNumber - 1) ? ">>> " : "    ";
                    contextLines.Add($"{marker}Line {i + 1}: {lines[i]}");
                }

                return string.Join("\n", contextLines);
            }
        }
        catch { }

        return "Could not extract error context";
    }
}