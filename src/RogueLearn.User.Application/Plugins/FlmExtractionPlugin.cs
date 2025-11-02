using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RogueLearn.User.Application.Plugins;

public class FlmExtractionPlugin : IFlmExtractionPlugin
{
    private readonly Kernel _kernel;
    private readonly ILogger<FlmExtractionPlugin> _logger;

    public FlmExtractionPlugin(Kernel kernel, ILogger<FlmExtractionPlugin> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<string> ExtractCurriculumJsonAsync(string rawText, CancellationToken cancellationToken = default)
    {
        var header = @"
Extract curriculum information from the following text and return it as JSON following this exact schema:

{
  ""program"": {
    ""programName"": ""string (max 255 chars)"",
    ""programCode"": ""string (max 50 chars, e.g., 'BIT_SE_K16D_K17A', 'BIT_SE_K16C', 'K16A')"",
    ""description"": ""string"",
    ""degreeLevel"": ""Bachelor"",
    ""totalCredits"": number,
    ""durationYears"": number

  },
  ""version"": {
    ""versionCode"": ""string (max 50 chars, use full date format like '2024-09-01' if date is available, otherwise use format like 'V1.0')"",
    ""effectiveYear"": number (year, e.g., 2022),
    ""description"": ""string (optional)"",
    ""isActive"": true
  },
  ""subjects"": [
    {
      ""subjectCode"": ""string (max 50 chars)"",
      ""subjectName"": ""string (max 255 chars)"",
      ""credits"": number (1-10),
      ""description"": ""string (optional)""
    }
  ],
  ""structure"": [
    {
      ""subjectCode"": ""string"",
      ""termNumber"": number (1-12),
      ""isMandatory"": true,
      ""prerequisiteSubjectCodes"": [""string""] (optional),
      ""prerequisitesText"": ""string (optional)""
    }
  ]
}

Important notes:
- degreeLevel: Use ""Associate"", ""Bachelor"", ""Master"", or ""Doctorate"" (enum string values)
- effectiveYear: Extract year from any date mentioned (e.g., from ""2022-10-26"" use 2022)
- versionCode: Use full date format (e.g., ""2024-09-01"") if an effective date or approval date is found in the text. If no date is available, generate a meaningful version code like ""V1.0""
- programCode: Accept various formats like 'BIT_SE_K16D_K17A', 'BIT_SE_K16C', 'BIT_SE_K15D', 'K16A'. If multiple student year codes are present, format as 'PROGRAM_SPECIALIZATION_YEAR1_YEAR2' (e.g., 'BIT_SE_K15D_K16A'). Keep original format if it follows university naming conventions.
- structure: Map each subject to a term/semester number, use 1 if not specified
- All string fields should be properly escaped for JSON

Text to extract from:
";

        var prompt = header + rawText + @"

Return only the JSON, no additional text or formatting.";

        try
        {
            var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var rawResponse = result.GetValue<string>() ?? string.Empty;
            _logger.LogInformation("Curriculum extractor raw response: {RawResponse}", rawResponse);
            return CleanToJson(rawResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract curriculum data using AI");
            return string.Empty;
        }
    }

    public async Task<string> ExtractSyllabusJsonAsync(string rawText, CancellationToken cancellationToken = default)
    {
        var header = @"Extract syllabus information and return as JSON with this structure:

{
  ""syllabusId"": number,
  ""subjectCode"": ""string"",
  ""syllabusName"": ""string"",
  ""syllabusEnglish"": ""string"",
  ""versionNumber"": number,
  ""noCredit"": number,
  ""degreeLevel"": ""string"",
  ""timeAllocation"": ""string"",
  ""preRequisite"": ""string"",
  ""description"": ""string"",
  ""studentTasks"": [""string""] ,
  ""tools"": [""string""] ,
  ""decisionNo"": ""string"",
  ""isApproved"": boolean,
  ""note"": ""string"",
  ""isActive"": boolean,
  ""approvedDate"": ""YYYY-MM-DD"",
  ""materials"": [{""materialDescription"": ""string"", ""author"": ""string"", ""publisher"": ""string"", ""publishedDate"": ""YYYY-MM-DD"", ""edition"": ""string"", ""isbn"": ""string"", ""isMainMaterial"": boolean, ""isHardCopy"": boolean, ""isOnline"": boolean, ""note"": ""string""}],
  ""content"": {
    ""courseDescription"": ""string"",
    ""weeklySchedule"": [{""weekNumber"": 1-10, ""topic"": ""string"", ""activities"": [""string""], ""readings"": [""string""], ""constructiveQuestions"": [{""question"": ""string"", ""answer"": ""string"", ""category"": ""string"", ""sessionNumber"": number (optional)}]}],
    ""assessments"": [{""name"": ""string"", ""type"": ""string"", ""weightPercentage"": integer, ""description"": ""string""}],
    ""courseLearningOutcomes"": [{""id"": ""string"", ""details"": ""string""}],
    ""requiredTexts"": [""string""],
    ""recommendedTexts"": [""string""],
    ""gradingPolicy"": ""string"",
    ""attendancePolicy"": ""string""
  }
}

RULES:
- versionNumber: Numeric YYYYMMDD from approvedDate; if missing, use current date in YYYYMMDD
- weeklySchedule: If text indicates 30 sessions, generate 5 weeks (1-5); otherwise 10 weeks (1-10). Distribute content logically
- constructiveQuestions: Attach questions to the appropriate week. If session numbers are available, use them to map questions to weeks; otherwise, group logically by topic.
- courseLearningOutcomes: Extract CLO id (e.g., ""CLO1"") and full details to populate LO Details
- Dates: Use YYYY-MM-DD format, null if missing
- Missing values: empty strings/arrays, false, 0, null for dates
- For the root field 'description', summarize into a concise overview (≤ 300 characters)
 - For each assessment's 'description', summarize concisely (≤ 300 characters)

Text: ";

        var prompt = header + rawText + @"

Return only JSON:";

        try
        {
            var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var rawResponse = result.GetValue<string>() ?? string.Empty;
            _logger.LogInformation("Syllabus extractor raw response: {RawResponse}", rawResponse);
            return CleanToJson(rawResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract syllabus data using AI");
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