// RogueLearn.User/src/RogueLearn.User.Application/Features/CurriculumImport/Queries/ValidateSyllabus/ValidateSyllabusQueryHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Plugins;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateSyllabus;

public class ValidateSyllabusQueryHandler : IRequestHandler<ValidateSyllabusQuery, ValidateSyllabusResponse>
{
    private readonly ICurriculumImportStorage _storage;
    private readonly FluentValidation.IValidator<SyllabusData> _validator;
    private readonly ILogger<ValidateSyllabusQueryHandler> _logger;
    // MODIFIED: Dependency is now the correct, specific interface.
    private readonly ISyllabusExtractionPlugin _syllabusPlugin;

    public ValidateSyllabusQueryHandler(
        ICurriculumImportStorage storage,
        FluentValidation.IValidator<SyllabusData> validator,
        ILogger<ValidateSyllabusQueryHandler> logger,
        // MODIFIED: Constructor now requires the correct interface.
        ISyllabusExtractionPlugin syllabusPlugin)
    {
        _storage = storage;
        _validator = validator;
        _logger = logger;
        _syllabusPlugin = syllabusPlugin;
    }

    public async Task<ValidateSyllabusResponse> Handle(ValidateSyllabusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting syllabus validation from text");

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
                extractedJson = await ExtractSyllabusData(request.RawText, cancellationToken);
                if (string.IsNullOrEmpty(extractedJson))
                {
                    return new ValidateSyllabusResponse
                    {
                        IsValid = false,
                        Message = "Failed to extract syllabus data from the provided text"
                    };
                }
            }

            SyllabusData? syllabusData;
            try
            {
                // MODIFIED: Added JsonStringEnumConverter for robust enum parsing.
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                syllabusData = JsonSerializer.Deserialize<SyllabusData>(extractedJson, options);
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

            var validationResult = await _validator.ValidateAsync(syllabusData, cancellationToken);

            var response = new ValidateSyllabusResponse
            {
                IsValid = validationResult.IsValid,
                ExtractedData = syllabusData,
                ValidationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
            };

            if (validationResult.IsValid)
            {
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
                Message = "An error occurred during validation"
            };
        }
    }

    private async Task<string> ExtractSyllabusData(string rawText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }
        // MODIFIED: This now calls the specific syllabus plugin.
        return await _syllabusPlugin.ExtractSyllabusJsonAsync(rawText, cancellationToken);
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
                await _storage.SaveSyllabusDataAsync(syllabusData.SubjectCode, syllabusData.VersionNumber, syllabusData, extractedData, inputHash, cancellationToken);
                _logger.LogInformation("Saved syllabus data for subject: {SubjectCode} version: {Version}",
                    syllabusData.SubjectCode, syllabusData.VersionNumber);
            }
            else
            {
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

    // MODIFICATION: All obsolete manual HTML parsing methods have been removed.
    // The handler now correctly relies on the AI plugin for all extraction logic.
}