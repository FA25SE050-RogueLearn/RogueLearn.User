// RogueLearn.User/src/RogueLearn.User.Application/Features/CurriculumImport/Queries/ValidateCurriculum/ValidateCurriculumQueryHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Plugins;

namespace RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;

public class ValidateCurriculumQueryHandler : IRequestHandler<ValidateCurriculumQuery, ValidateCurriculumResponse>
{
    private readonly ICurriculumImportStorage _storage;
    private readonly CurriculumImportDataValidator _validator;
    private readonly ILogger<ValidateCurriculumQueryHandler> _logger;
    // MODIFIED: Dependency changed from the obsolete IFlmExtractionPlugin to the new, specific plugin.
    private readonly ICurriculumExtractionPlugin _flmPlugin;

    public ValidateCurriculumQueryHandler(
        ICurriculumImportStorage storage,
        CurriculumImportDataValidator validator,
        ILogger<ValidateCurriculumQueryHandler> logger,
        // MODIFIED: Constructor now requires the correct interface.
        ICurriculumExtractionPlugin flmPlugin)
    {
        _storage = storage;
        _validator = validator;
        _logger = logger;
        _flmPlugin = flmPlugin;
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
                // MODIFIED: Method call is updated to use the new specific plugin.
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
        // MODIFIED: This now calls the specific curriculum plugin.
        return await _flmPlugin.ExtractCurriculumJsonAsync(rawText);
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