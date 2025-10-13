using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;

public class ValidateCurriculumQueryHandler : IRequestHandler<ValidateCurriculumQuery, ValidateCurriculumResponse>
{
    private readonly Kernel _kernel;
    private readonly CurriculumImportDataValidator _validator;
    private readonly ILogger<ValidateCurriculumQueryHandler> _logger;
    private readonly ICurriculumImportStorage _storage;

    public ValidateCurriculumQueryHandler(
        Kernel kernel,
        CurriculumImportDataValidator validator,
        ILogger<ValidateCurriculumQueryHandler> logger,
        ICurriculumImportStorage storage)
    {
        _kernel = kernel;
        _validator = validator;
        _logger = logger;
        _storage = storage;
    }

    public async Task<ValidateCurriculumResponse> Handle(ValidateCurriculumQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting curriculum validation from text");

            // Step 0: Compute hash and attempt to load cached JSON to save tokens
            var rawText = request.RawText ?? string.Empty;
            var rawTextHash = ComputeSha256Hash(rawText);
            string? extractedJson = null;
            try
            {
                extractedJson = await _storage.TryGetByHashJsonAsync(
                    bucketName: "curriculum-imports",
                    rawTextHash: rawTextHash,
                    cancellationToken: cancellationToken);
                if (!string.IsNullOrWhiteSpace(extractedJson))
                {
                    _logger.LogInformation("Loaded curriculum JSON from cache by hash, skipping AI extraction.");
                }
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "Failed to retrieve cached JSON by hash; will attempt AI extraction.");
            }

            // Step 1: Extract structured data using AI only if cache miss
            if (string.IsNullOrWhiteSpace(extractedJson))
            {
                extractedJson = await ExtractCurriculumDataAsync(rawText);
            }
            if (string.IsNullOrEmpty(extractedJson))
            {
                return new ValidateCurriculumResponse
                {
                    IsValid = false,
                    Message = "Failed to extract curriculum data from the provided text"
                };
            }

            // Step 2: Parse JSON (case-insensitive to handle camelCase keys)
            CurriculumImportData? curriculumData;
            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                curriculumData = JsonSerializer.Deserialize<CurriculumImportData>(extractedJson, jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse extracted JSON");
                return new ValidateCurriculumResponse
                {
                    IsValid = false,
                    Message = "Failed to parse extracted curriculum data"
                };
            }

            if (curriculumData == null)
            {
                return new ValidateCurriculumResponse
                {
                    IsValid = false,
                    Message = "No curriculum data was extracted"
                };
            }
            _logger.LogInformation("Processed curriculum data: {@CurriculumData}", curriculumData);

            // Step 2.5: Check storage by hash; use stored JSON if matches
            try
            {
                var programCode = curriculumData.Program?.ProgramCode ?? string.Empty;
                var versionCode = curriculumData.Version?.VersionCode ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(programCode) && !string.IsNullOrWhiteSpace(versionCode))
                {
                    var existingMeta = await _storage.TryGetLatestMetaJsonAsync(
                        bucketName: "curriculum-imports",
                        programCode: programCode,
                        versionCode: versionCode,
                        cancellationToken: cancellationToken);

                    bool useStoredJson = false;
                    if (!string.IsNullOrWhiteSpace(existingMeta))
                    {
                        try
                        {
                            using var metaDoc = JsonDocument.Parse(existingMeta);
                            var root = metaDoc.RootElement;
                            var storedHash = root.TryGetProperty("rawTextHash", out var h) ? h.GetString() : null;
                            if (!string.IsNullOrWhiteSpace(storedHash) && string.Equals(storedHash, rawTextHash, StringComparison.OrdinalIgnoreCase))
                            {
                                useStoredJson = true;
                            }
                        }
                        catch (Exception metaEx)
                        {
                            _logger.LogWarning(metaEx, "Failed to parse existing meta json");
                        }
                    }

                    if (useStoredJson)
                    {
                        // First try versioned-by-hash JSON under program/version
                        var storedJson = await _storage.TryGetVersionedByHashJsonAsync(
                            bucketName: "curriculum-imports",
                            programCode: programCode,
                            versionCode: versionCode,
                            rawTextHash: rawTextHash,
                            cancellationToken: cancellationToken);

                        if (!string.IsNullOrWhiteSpace(storedJson))
                        {
                            try
                            {
                                var jsonOptions2 = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                                var storedData = JsonSerializer.Deserialize<CurriculumImportData>(storedJson, jsonOptions2);
                                if (storedData != null)
                                {
                                    curriculumData = storedData;
                                    extractedJson = storedJson;
                                    _logger.LogInformation("Using stored curriculum JSON since raw text hash matches.");
                                }
                            }
                            catch (Exception parseStoredEx)
                            {
                                _logger.LogWarning(parseStoredEx, "Failed to parse stored JSON; proceeding with extracted content.");
                            }
                        }
                        else
                        {
                            // Fallback to latest.json under program/version
                            storedJson = await _storage.TryGetLatestJsonAsync(
                                bucketName: "curriculum-imports",
                                programCode: programCode,
                                versionCode: versionCode,
                                cancellationToken: cancellationToken);

                            if (!string.IsNullOrWhiteSpace(storedJson))
                            {
                                try
                                {
                                    var jsonOptions2 = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                                    var storedData = JsonSerializer.Deserialize<CurriculumImportData>(storedJson, jsonOptions2);
                                    if (storedData != null)
                                    {
                                        curriculumData = storedData;
                                        extractedJson = storedJson;
                                        _logger.LogInformation("Using latest stored curriculum JSON.");
                                    }
                                }
                                catch (Exception parseStoredEx)
                                {
                                    _logger.LogWarning(parseStoredEx, "Failed to parse latest stored JSON; proceeding with extracted content.");
                                }
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Skipping storage check due to missing programCode or versionCode");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed during storage check for curriculum import");
            }

            // Step 3: Validate extracted data FIRST
            var validationResult = await _validator.ValidateAsync(curriculumData, cancellationToken);
            
            var response = new ValidateCurriculumResponse
            {
                IsValid = validationResult.IsValid,
                ExtractedData = curriculumData,
                ValidationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
            };

            // Step 4: Only save to storage if validation passes
            if (validationResult.IsValid)
            {
                try
                {
                    var programCode = curriculumData.Program?.ProgramCode ?? string.Empty;
                    var versionCode = curriculumData.Version?.VersionCode ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(programCode) && !string.IsNullOrWhiteSpace(versionCode))
                    {
                        await _storage.SaveLatestAsync(
                            bucketName: "curriculum-imports",
                            programCode: programCode, // Use the full key as the program code
                            versionCode: versionCode, // Use the full key as the version code
                            jsonContent: extractedJson,
                            rawTextContent: rawText,
                            rawTextHash: rawTextHash,
                            cancellationToken: cancellationToken);
                        
                        _logger.LogInformation("Curriculum data saved to storage after successful validation");
                    }
                    else
                    {
                        _logger.LogWarning("Skipping storage save due to missing programCode or versionCode");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save curriculum data to storage after validation");
                }
                
                response.Message = "Curriculum data is valid and ready for import";
            }
            else
            {
                response.Message = "Curriculum data validation failed";
            }

            _logger.LogInformation("Curriculum validation completed");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during curriculum validation");
            return new ValidateCurriculumResponse
            {
                IsValid = false,
                Message = "An error occurred during curriculum validation"
            };
        }
    }

    private async Task<string> ExtractCurriculumDataAsync(string rawText)
    {
        var prompt = $@"
Extract curriculum information from the following text and return it as JSON following this exact schema:

{{
  ""program"": {{
    ""programName"": ""string (max 255 chars)"",
    ""programCode"": ""string (max 50 chars, e.g., 'BIT_SE_K16D_K17A', 'BIT_SE_K16C', 'K16A')"",
    ""description"": ""string"",
    ""degreeLevel"": ""Bachelor"",
    ""totalCredits"": number,
    ""durationYears"": number

  }},
  ""version"": {{
    ""versionCode"": ""string (max 50 chars, use full date format like '2024-09-01' if date is available, otherwise use format like 'V1.0')"",
    ""effectiveYear"": number (year, e.g., 2022),
    ""description"": ""string (optional)"",
    ""isActive"": true
  }},
  ""subjects"": [
    {{
      ""subjectCode"": ""string (max 50 chars)"",
      ""subjectName"": ""string (max 255 chars)"",
      ""credits"": number (1-10),
      ""description"": ""string (optional)""
    }}
  ],
  ""structure"": [
    {{
      ""subjectCode"": ""string"",
      ""termNumber"": number (1-12),
      ""isMandatory"": true,
      ""prerequisiteSubjectCodes"": [""string""] (optional),
      ""prerequisitesText"": ""string (optional)""
    }}
  ]
}}

Important notes:
- degreeLevel: Use ""Associate"", ""Bachelor"", ""Master"", or ""Doctorate"" (enum string values)
- effectiveYear: Extract year from any date mentioned (e.g., from ""2022-10-26"" use 2022)
- versionCode: Use full date format (e.g., ""2024-09-01"") if an effective date or approval date is found in the text. If no date is available, generate a meaningful version code like ""V1.0""
- programCode: Accept various formats like 'BIT_SE_K16D_K17A', 'BIT_SE_K16C', 'BIT_SE_K15D', 'K16A'. If multiple student year codes are present, format as 'PROGRAM_SPECIALIZATION_YEAR1_YEAR2' (e.g., 'BIT_SE_K15D_K16A'). Keep original format if it follows university naming conventions.
- structure: Map each subject to a term/semester number, use 1 if not specified
- All string fields should be properly escaped for JSON

Text to extract from:
{rawText}

Return only the JSON, no additional text or formatting.";

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
            _logger.LogError(ex, "Failed to extract curriculum data using AI");
            return string.Empty;
        }
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}