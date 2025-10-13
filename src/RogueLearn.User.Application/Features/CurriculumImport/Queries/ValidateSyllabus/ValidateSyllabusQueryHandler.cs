using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateSyllabus;

public class ValidateSyllabusQueryHandler : IRequestHandler<ValidateSyllabusQuery, ValidateSyllabusResponse>
{
    private readonly Kernel _kernel;
    private readonly SyllabusDataValidator _validator;
    private readonly ICurriculumImportStorage _curriculumStorage;
    private readonly ILogger<ValidateSyllabusQueryHandler> _logger;

    public ValidateSyllabusQueryHandler(
        Kernel kernel,
        SyllabusDataValidator validator,
        ICurriculumImportStorage curriculumStorage,
        ILogger<ValidateSyllabusQueryHandler> logger)
    {
        _kernel = kernel;
        _validator = validator;
        _curriculumStorage = curriculumStorage;
        _logger = logger;
    }

    public async Task<ValidateSyllabusResponse> Handle(ValidateSyllabusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting syllabus validation from text");

            // Step 1: Check cache first
            var inputHash = ComputeSha256Hash(request.RawText);
            var cachedData = await TryGetCachedDataAsync(inputHash, cancellationToken);
            
            string extractedJson;
            if (!string.IsNullOrEmpty(cachedData))
            {
                _logger.LogInformation("Using cached syllabus data for validation");
                extractedJson = cachedData;
            }
            else
            {
                // Step 2: Extract structured data using AI
                extractedJson = await ExtractSyllabusDataAsync(request.RawText);
                if (string.IsNullOrEmpty(extractedJson))
                {
                    return new ValidateSyllabusResponse
                    {
                        IsValid = false,
                        Message = "Failed to extract syllabus data from the provided text"
                    };
                }
            }

            // Step 3: Parse JSON
            SyllabusData? syllabusData;
            try
            {
                syllabusData = JsonSerializer.Deserialize<SyllabusData>(extractedJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse extracted JSON");
                return new ValidateSyllabusResponse
                {
                    IsValid = false,
                    Message = "Failed to parse extracted syllabus data"
                };
            }

            if (syllabusData == null)
            {
                return new ValidateSyllabusResponse
                {
                    IsValid = false,
                    Message = "No syllabus data was extracted"
                };
            }

            // Step 4: Validate extracted data first
            var validationResult = await _validator.ValidateAsync(syllabusData, cancellationToken);
            
            var response = new ValidateSyllabusResponse
            {
                IsValid = validationResult.IsValid,
                ExtractedData = syllabusData,
                ValidationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
            };

            if (validationResult.IsValid)
            {
                // Only save to storage if validation passes
                if (!string.IsNullOrEmpty(syllabusData.SubjectCode))
                {
                    await SaveDataToStorageAsync(inputHash, extractedJson, syllabusData, cancellationToken);
                }
                response.Message = "Syllabus data is valid and ready for import";
            }
            else
            {
                response.Message = "Syllabus data validation failed";
            }

            _logger.LogInformation("Syllabus validation completed");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during syllabus validation");
            return new ValidateSyllabusResponse
            {
                IsValid = false,
                Message = "An error occurred during syllabus validation"
            };
        }
    }

    private async Task<string> ExtractSyllabusDataAsync(string rawText)
    {
        var prompt = $@"Extract syllabus information and return as JSON with this structure:

{{
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
  ""studentTasks"": [""string""],
  ""tools"": [""string""],
  ""decisionNo"": ""string"",
  ""isApproved"": boolean,
  ""note"": ""string"",
  ""isActive"": boolean,
  ""approvedDate"": ""YYYY-MM-DD"",
  ""materials"": [{{""materialDescription"": ""string"", ""author"": ""string"", ""publisher"": ""string"", ""publishedDate"": ""YYYY-MM-DD"", ""edition"": ""string"", ""isbn"": ""string"", ""isMainMaterial"": boolean, ""isHardCopy"": boolean, ""isOnline"": boolean, ""note"": ""string""}}],
  ""content"": {{
    ""courseDescription"": ""string"",
    ""weeklySchedule"": [{{""weekNumber"": 1-10, ""topic"": ""string"", ""activities"": [""string""], ""readings"": [""string""]}}],
    ""assessments"": [{{""name"": ""string"", ""type"": ""string"", ""weightPercentage"": integer, ""description"": ""string""}}],
    ""constructiveQuestions"": [{{""question"": ""string"", ""answer"": ""string"", ""category"": ""string""}}],
    ""requiredTexts"": [""string""],
    ""recommendedTexts"": [""string""],
    ""gradingPolicy"": ""string"",
    ""attendancePolicy"": ""string""
  }}
}}

RULES:
- versionNumber: Numeric YYYYMMDD from approvedDate; if missing, use current date in YYYYMMDD
- weeklySchedule: If text indicates 30 sessions, generate 5 weeks (1-5); otherwise 10 weeks (1-10). Distribute content logically
- Dates: Use YYYY-MM-DD format, null if missing
- Missing values: empty strings/arrays, false, 0, null for dates
- For the root field 'description', summarize into a concise overview (≤ 300 characters)
 - For each assessment's 'description', summarize concisely (≤ 300 characters)

Text: {rawText}

Return only JSON:";

        try
        {
            var result = await _kernel.InvokePromptAsync(prompt);
            _logger.LogInformation("Raw AI response: {RawResponse}", result.GetValue<string>() ?? string.Empty);

            var rawResponse = result.GetValue<string>() ?? string.Empty;

            // Clean up the response - remove markdown and isolate the JSON block robustly
            var cleanedResponse = rawResponse.Trim();

            // If the response contains code fences, strip them
            if (cleanedResponse.StartsWith("```"))
            {
                // Remove leading code fence (``` or ```json)
                var firstNewline = cleanedResponse.IndexOf('\n');
                if (firstNewline > -1)
                {
                    cleanedResponse = cleanedResponse.Substring(firstNewline + 1);
                }
            }
            if (cleanedResponse.EndsWith("```"))
            {
                // Remove trailing code fence
                var lastFenceIndex = cleanedResponse.LastIndexOf("```", StringComparison.Ordinal);
                if (lastFenceIndex > -1)
                {
                    cleanedResponse = cleanedResponse.Substring(0, lastFenceIndex);
                }
            }

            // Extract content between first '{' and last '}' to ensure only JSON remains
            var startIdx = cleanedResponse.IndexOf('{');
            var endIdx = cleanedResponse.LastIndexOf('}');
            if (startIdx >= 0 && endIdx > startIdx)
            {
                cleanedResponse = cleanedResponse.Substring(startIdx, endIdx - startIdx + 1);
            }

            return cleanedResponse.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract syllabus data using AI");
            return string.Empty;
        }
    }

    private async Task<string?> TryGetCachedDataAsync(string inputHash, CancellationToken cancellationToken)
    {
        try
        {
            return await _curriculumStorage.TryGetCachedSyllabusDataAsync(inputHash, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve cached syllabus validation data");
            return null;
        }
    }

    private async Task SaveDataToStorageAsync(string inputHash, string extractedData, SyllabusData? syllabusData, CancellationToken cancellationToken)
    {
        try
        {
            if (syllabusData != null && !string.IsNullOrEmpty(syllabusData.SubjectCode))
            {
                // Use subject code and version for organized storage
                await _curriculumStorage.SaveSyllabusDataAsync(syllabusData.SubjectCode, syllabusData.VersionNumber, syllabusData, extractedData, inputHash, cancellationToken);
                _logger.LogInformation("Saved syllabus data for subject: {SubjectCode} version: {Version}", 
                    syllabusData.SubjectCode, syllabusData.VersionNumber);
            }
            else
            {
                // For temporary data, we'll use the curriculum storage directly since ISyllabusImportStorage doesn't have SaveTemporaryDataAsync
                _logger.LogInformation("Syllabus data saved with input hash for caching purposes");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save syllabus validation data");
        }
    }

    private string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}