using MediatR;
using Microsoft.Extensions.Logging;
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
using RogueLearn.User.Application.Plugins;

namespace RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportSyllabus;

public class ImportSyllabusCommandHandler : IRequestHandler<ImportSyllabusCommand, ImportSyllabusResponse>
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly ISyllabusVersionRepository _syllabusVersionRepository;
    private readonly ICurriculumImportStorage _storage;
    private readonly SyllabusDataValidator _validator;
    private readonly ILogger<ImportSyllabusCommandHandler> _logger;
    private readonly IFlmExtractionPlugin _flmPlugin;

    public ImportSyllabusCommandHandler(
        ISubjectRepository subjectRepository,
        ISyllabusVersionRepository syllabusVersionRepository,
        ICurriculumImportStorage storage,
        SyllabusDataValidator validator,
        ILogger<ImportSyllabusCommandHandler> logger,
        IFlmExtractionPlugin flmPlugin)
    {
        _subjectRepository = subjectRepository;
        _syllabusVersionRepository = syllabusVersionRepository;
        _storage = storage;
        _validator = validator;
        _logger = logger;
        _flmPlugin = flmPlugin;
    }

    private async Task<SyllabusData?> TryGetCachedDataAsync(string textHash, CancellationToken cancellationToken)
    {
        try
        {
            var cachedJson = await _storage.TryGetByHashJsonAsync("curriculum-imports", textHash, cancellationToken);
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
            await _storage.SaveLatestAsync(
                "curriculum-imports",
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
        return await _flmPlugin.ExtractSyllabusJsonAsync(rawText);
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