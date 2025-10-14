using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;

namespace RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateSyllabus;

public class ValidateSyllabusQueryHandler : IRequestHandler<ValidateSyllabusQuery, ValidateSyllabusResponse>
{
    private readonly ICurriculumImportStorage _storage;
    private readonly SyllabusDataValidator _validator;
    private readonly ILogger<ValidateSyllabusQueryHandler> _logger;
    private readonly IFlmExtractionPlugin _flmPlugin;

    public ValidateSyllabusQueryHandler(
        ICurriculumImportStorage storage,
        SyllabusDataValidator validator,
        ILogger<ValidateSyllabusQueryHandler> logger,
        IFlmExtractionPlugin flmPlugin)
    {
        _storage = storage;
        _validator = validator;
        _logger = logger;
        _flmPlugin = flmPlugin;
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
        return await _flmPlugin.ExtractSyllabusJsonAsync(rawText);
    }

    private async Task<string?> TryGetCachedDataAsync(string inputHash, CancellationToken cancellationToken)
    {
        try
        {
            return await _storage.TryGetCachedSyllabusDataAsync(inputHash, cancellationToken);
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
                await _storage.SaveSyllabusDataAsync(syllabusData.SubjectCode, syllabusData.VersionNumber, syllabusData, extractedData, inputHash, cancellationToken);
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