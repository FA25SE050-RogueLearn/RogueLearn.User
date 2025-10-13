using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateSyllabus;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportSyllabus;

public class ImportSyllabusCommandHandler : IRequestHandler<ImportSyllabusCommand, ImportSyllabusResponse>
{
    private readonly Kernel _kernel;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ISyllabusVersionRepository _syllabusVersionRepository;
    private readonly ICurriculumImportStorage _curriculumImportStorage;
    private readonly SyllabusDataValidator _validator;
    private readonly ILogger<ImportSyllabusCommandHandler> _logger;

    public ImportSyllabusCommandHandler(
        Kernel kernel,
        ISubjectRepository subjectRepository,
        ISyllabusVersionRepository syllabusVersionRepository,
        ICurriculumImportStorage curriculumImportStorage,
        SyllabusDataValidator validator,
        ILogger<ImportSyllabusCommandHandler> logger)
    {
        _kernel = kernel;
        _subjectRepository = subjectRepository;
        _syllabusVersionRepository = syllabusVersionRepository;
        _curriculumImportStorage = curriculumImportStorage;
        _validator = validator;
        _logger = logger;
    }

    private async Task<SyllabusData?> TryGetCachedDataAsync(string textHash, CancellationToken cancellationToken)
    {
        try
        {
            var cachedJson = await _curriculumImportStorage.TryGetByHashJsonAsync("syllabus-imports", textHash, cancellationToken);
            _logger.LogInformation("Cache lookup for hash: {Hash}, found: {Found}", textHash, !string.IsNullOrEmpty(cachedJson));
            if (!string.IsNullOrEmpty(cachedJson))
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                return JsonSerializer.Deserialize<SyllabusData>(cachedJson, jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve cached data for hash: {Hash}", textHash);
        }
        return null;
    }

    private async Task SaveDataToStorageAsync(SyllabusData data, string rawText, string textHash, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await _curriculumImportStorage.SaveLatestAsync(
                "syllabus-imports",
                data.SubjectCode,
                data.VersionNumber.ToString(),
                json,
                rawText,
                textHash,
                cancellationToken);
            _logger.LogInformation("Saved syllabus data to storage with hash: {Hash}", textHash);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save syllabus data to storage for hash: {Hash}", textHash);
        }
    }

    public async Task<ImportSyllabusResponse> Handle(ImportSyllabusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting syllabus import from text");

            // Step 1: Check if we have cached data for this text
            var textHash = ComputeSha256Hash(request.RawText);
            var cachedData = await TryGetCachedDataAsync(textHash, cancellationToken);

            SyllabusData? syllabusData;

            if (cachedData != null)
            {
                _logger.LogInformation("Using cached syllabus data for hash: {Hash}", textHash);
                syllabusData = cachedData;
            }
            else
            {
                // Step 2: Extract structured data using AI
                var extractedJson = await ExtractSyllabusDataAsync(request.RawText);
                if (string.IsNullOrEmpty(extractedJson))
                {
                    return new ImportSyllabusResponse
                    {
                        IsSuccess = false,
                        Message = "Failed to extract syllabus data from the provided text"
                    };
                }

                // Step 3: Parse JSON
                try
                {
                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new JsonStringEnumConverter() }
                    };
                    syllabusData = JsonSerializer.Deserialize<SyllabusData>(extractedJson, jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse extracted JSON");
                    return new ImportSyllabusResponse
                    {
                        IsSuccess = false,
                        Message = "Failed to parse extracted syllabus data"
                    };
                }

                if (syllabusData == null)
                {
                    return new ImportSyllabusResponse
                    {
                        IsSuccess = false,
                        Message = "No syllabus data was extracted"
                    };
                }

                // Step 4: Save extracted data to storage for future use
                await SaveDataToStorageAsync(syllabusData, request.RawText, textHash, cancellationToken);
            }

            // Step 5: Validate extracted data
            var validationResult = await _validator.ValidateAsync(syllabusData, cancellationToken);
            if (!validationResult.IsValid)
            {
                return new ImportSyllabusResponse
                {
                    IsSuccess = false,
                    Message = "Validation failed",
                    ValidationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
                };
            }

            // Step 6: Map and persist data
            var result = await PersistSyllabusDataAsync(syllabusData, request.CreatedBy, cancellationToken);

            _logger.LogInformation("Syllabus import completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during syllabus import");
            return new ImportSyllabusResponse
            {
                IsSuccess = false,
                Message = "An error occurred during syllabus import"
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
  ""learningOutcomes"": [{{""cloNumber"": number, ""cloName"": ""string"", ""cloDetails"": ""string"", ""loDetails"": ""string""}}],
  ""constructiveQuestions"": [{{""question"": ""string"", ""answer"": ""string"", ""category"": ""string""}}],
  ""assessments"": [{{""type"": ""string"", ""description"": ""string"", ""weightPercentage"": number, ""dueDate"": ""YYYY-MM-DD"", ""instructions"": ""string""}}],
  ""content"": {{
    ""courseDescription"": ""string"",
    ""learningOutcomes"": [""string""],
    ""weeklySchedule"": [{{""weekNumber"": 1-10, ""topic"": ""string"", ""activities"": [""string""], ""readings"": [""string""]}}],
    ""assessments"": [{{""name"": ""string"", ""type"": ""string"", ""weightPercentage"": number, ""description"": ""string""}}],
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

    private async Task<ImportSyllabusResponse> PersistSyllabusDataAsync(
        SyllabusData data, 
        Guid? createdBy, 
        CancellationToken cancellationToken)
    {
        var response = new ImportSyllabusResponse { IsSuccess = true };

        // Find or create subject
        var subject = await _subjectRepository
            .FirstOrDefaultAsync(s => s.SubjectCode == data.SubjectCode, cancellationToken);

        if (subject == null)
        {
            throw new NotFoundException($"Subject with code '{data.SubjectCode}' not found. Please import curriculum first.");
        }

        response.SubjectCode = subject.SubjectCode;
        response.SubjectId = subject.Id;

        // Check if syllabus version already exists
        var existingVersion = await _syllabusVersionRepository.FirstOrDefaultAsync(
            v => v.SubjectId == subject.Id && v.VersionNumber == data.VersionNumber, 
            cancellationToken);

        if (existingVersion != null)
        {
            _logger.LogWarning("Syllabus version {VersionNumber} for subject {SubjectCode} already exists. Skipping import.", 
                data.VersionNumber, data.SubjectCode);
            
            return new ImportSyllabusResponse
            {
                IsSuccess = false,
                Message = $"Syllabus version '{data.VersionNumber}' for subject '{data.SubjectCode}' already exists. Import skipped to prevent duplicates."
            };
        }

        // Create syllabus version
        var syllabusVersion = new SyllabusVersion
        {
            SubjectId = subject.Id,
            VersionNumber = data.VersionNumber,
            Content = JsonSerializer.Serialize(data.Content),
            EffectiveDate = data.EffectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true,
            CreatedBy = createdBy
        };

        await _syllabusVersionRepository.AddAsync(syllabusVersion, cancellationToken);
        response.SyllabusVersionId = syllabusVersion.Id;

        response.Message = "Syllabus imported successfully";
        return response;
    }

    private string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}