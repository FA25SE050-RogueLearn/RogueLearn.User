using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportCurriculum;

public class ImportCurriculumCommandHandler : IRequestHandler<ImportCurriculumCommand, ImportCurriculumResponse>
{
    private readonly Kernel _kernel;
    private readonly ICurriculumProgramRepository _curriculumProgramRepository;
    private readonly ICurriculumVersionRepository _curriculumVersionRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly ICurriculumImportStorage _curriculumImportStorage;
    private readonly CurriculumImportDataValidator _validator;
    private readonly ILogger<ImportCurriculumCommandHandler> _logger;

    public ImportCurriculumCommandHandler(
        Kernel kernel,
        ICurriculumProgramRepository curriculumProgramRepository,
        ICurriculumVersionRepository curriculumVersionRepository,
        ISubjectRepository subjectRepository,
        ICurriculumStructureRepository curriculumStructureRepository,
        ICurriculumImportStorage curriculumImportStorage,
        CurriculumImportDataValidator validator,
        ILogger<ImportCurriculumCommandHandler> logger)
    {
        _kernel = kernel;
        _curriculumProgramRepository = curriculumProgramRepository;
        _curriculumVersionRepository = curriculumVersionRepository;
        _subjectRepository = subjectRepository;
        _curriculumStructureRepository = curriculumStructureRepository;
        _curriculumImportStorage = curriculumImportStorage;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ImportCurriculumResponse> Handle(ImportCurriculumCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting curriculum import from text");

            // Step 1: Check if we have cached data for this text
            var textHash = ComputeSha256Hash(request.RawText);
            var cachedData = await TryGetCachedDataAsync(textHash, cancellationToken);

            CurriculumImportData? curriculumData;

            if (cachedData != null)
            {
                _logger.LogInformation("Using cached curriculum data for hash: {Hash}", textHash);
                curriculumData = cachedData;
            }
            else
            {
                // Step 2: Extract structured data using AI
                var extractedJson = await ExtractCurriculumDataAsync(request.RawText);
                if (string.IsNullOrEmpty(extractedJson))
                {
                    return new ImportCurriculumResponse
                    {
                        IsSuccess = false,
                        Message = "Failed to extract curriculum data from the provided text"
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
                    curriculumData = JsonSerializer.Deserialize<CurriculumImportData>(extractedJson, jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse extracted JSON");
                    return new ImportCurriculumResponse
                    {
                        IsSuccess = false,
                        Message = "Failed to parse extracted curriculum data"
                    };
                }

                if (curriculumData == null)
                {
                    return new ImportCurriculumResponse
                    {
                        IsSuccess = false,
                        Message = "No curriculum data was extracted"
                    };
                }

                // Step 4: Save extracted data to storage for future use
                await SaveDataToStorageAsync(curriculumData, request.RawText, textHash, cancellationToken);
            }

            // Step 5: Validate extracted data
            var validationResult = await _validator.ValidateAsync(curriculumData, cancellationToken);
            if (!validationResult.IsValid)
            {
                return new ImportCurriculumResponse
                {
                    IsSuccess = false,
                    Message = "Validation failed",
                    ValidationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
                };
            }

            // Step 6: Map and persist data
            var result = await PersistCurriculumDataAsync(curriculumData, request.CreatedBy, cancellationToken);

            _logger.LogInformation("Curriculum import completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during curriculum import");
            return new ImportCurriculumResponse
            {
                IsSuccess = false,
                Message = "An error occurred during curriculum import"
            };
        }
    }

    private async Task<CurriculumImportData?> TryGetCachedDataAsync(string textHash, CancellationToken cancellationToken)
    {
        try
        {
            var cachedJson = await _curriculumImportStorage.TryGetByHashJsonAsync("curriculum-imports", textHash, cancellationToken);
            _logger.LogInformation("Cache lookup for hash: {Hash}, found: {Found}", textHash, cachedJson != null);
            if (!string.IsNullOrEmpty(cachedJson))
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                return JsonSerializer.Deserialize<CurriculumImportData>(cachedJson, jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve cached data for hash: {Hash}", textHash);
        }
        return null;
    }

    private async Task SaveDataToStorageAsync(CurriculumImportData data, string rawText, string textHash, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await _curriculumImportStorage.SaveLatestAsync(
                "curriculum-imports",
                data.Program.ProgramCode,
                data.Version.VersionCode,
                json,
                rawText,
                textHash,
                cancellationToken);
            _logger.LogInformation("Saved curriculum data to storage with hash: {Hash}", textHash);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save curriculum data to storage for hash: {Hash}", textHash);
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

    private async Task<ImportCurriculumResponse> PersistCurriculumDataAsync(
        CurriculumImportData data,
        Guid? createdBy,
        CancellationToken cancellationToken)
    {
        var response = new ImportCurriculumResponse { IsSuccess = true };

        // Create or get curriculum program
        var existingProgram = await _curriculumProgramRepository.FirstOrDefaultAsync(p => p.ProgramCode == data.Program.ProgramCode, cancellationToken);
        CurriculumProgram curriculumProgram;

        if (existingProgram == null)
        {
            curriculumProgram = new CurriculumProgram
            {
                ProgramName = data.Program.ProgramName,
                ProgramCode = data.Program.ProgramCode,
                Description = data.Program.Description,
                DegreeLevel = data.Program.DegreeLevel,
                TotalCredits = data.Program.TotalCredits,
                DurationYears = data.Program.DurationYears,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            curriculumProgram = await _curriculumProgramRepository.AddAsync(curriculumProgram, cancellationToken);
            _logger.LogInformation("Created new curriculum program with ID: {ProgramId}", curriculumProgram.Id);
        }
        else
        {
            curriculumProgram = existingProgram;
        }

        response.CurriculumProgramId = curriculumProgram.Id;

        // Check if curriculum version already exists
        var existingVersion = await _curriculumVersionRepository.FirstOrDefaultAsync(
            v => v.ProgramId == curriculumProgram.Id && v.VersionCode == data.Version.VersionCode, 
            cancellationToken);

        CurriculumVersion curriculumVersion;

        if (existingVersion != null)
        {
            _logger.LogWarning("Curriculum version {VersionCode} for program {ProgramCode} already exists. Skipping import.", 
                data.Version.VersionCode, data.Program.ProgramCode);
            
            return new ImportCurriculumResponse
            {
                IsSuccess = false,
                Message = $"Curriculum version '{data.Version.VersionCode}' for program '{data.Program.ProgramCode}' already exists. Import skipped to prevent duplicates."
            };
        }

        // Create curriculum version
        curriculumVersion = new CurriculumVersion
        {
            ProgramId = curriculumProgram.Id,
            VersionCode = data.Version.VersionCode,
            EffectiveYear = data.Version.EffectiveYear,
            IsActive = data.Version.IsActive,
            Description = data.Version.Description,
            CreatedAt = DateTimeOffset.UtcNow
        };
        curriculumVersion = await _curriculumVersionRepository.AddAsync(curriculumVersion, cancellationToken);
        _logger.LogInformation("Created new curriculum version with ID: {VersionId}", curriculumVersion.Id);

        response.CurriculumVersionId = curriculumVersion.Id;

        // Create or update subjects
        foreach (var subjectData in data.Subjects)
        {
            var existingSubject = await _subjectRepository.FirstOrDefaultAsync(s => s.SubjectCode == subjectData.SubjectCode, cancellationToken);
            Subject subject;

            if (existingSubject != null)
            {
                subject = existingSubject;
                subject.SubjectName = subjectData.SubjectName;
                subject.Credits = subjectData.Credits;
                subject.Description = subjectData.Description;
                subject.UpdatedAt = DateTime.UtcNow;
                await _subjectRepository.UpdateAsync(subject, cancellationToken);
                _logger.LogInformation("Updated subject with ID: {SubjectId}", subject.Id);
            }
            else
            {
                subject = new Subject
                {
                    SubjectCode = subjectData.SubjectCode,
                    SubjectName = subjectData.SubjectName,
                    Credits = subjectData.Credits,
                    Description = subjectData.Description,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                subject = await _subjectRepository.AddAsync(subject, cancellationToken);
                _logger.LogInformation("Created new subject with ID: {SubjectId}", subject.Id);
            }

            response.SubjectIds.Add(subject.Id);
        }

        // Create curriculum structure
        foreach (var structureData in data.Structure)
        {
            var subject = await _subjectRepository.FirstOrDefaultAsync(s => s.SubjectCode == structureData.SubjectCode, cancellationToken);
            if (subject == null) continue;

            // Get prerequisite subject IDs
            var prerequisiteIds = new List<Guid>();
            if (structureData.PrerequisiteSubjectCodes?.Any() == true)
            {
                foreach (var prereqCode in structureData.PrerequisiteSubjectCodes)
                {
                    var prereqSubject = await _subjectRepository.FirstOrDefaultAsync(s => s.SubjectCode == prereqCode, cancellationToken);
                    if (prereqSubject != null)
                    {
                        prerequisiteIds.Add(prereqSubject.Id);
                    }
                }
            }

            var structure = new Domain.Entities.CurriculumStructure
            {
                CurriculumVersionId = curriculumVersion.Id,
                SubjectId = subject.Id,
                TermNumber = structureData.TermNumber,
                IsMandatory = structureData.IsMandatory,
                PrerequisiteSubjectIds = prerequisiteIds.ToArray(),
                PrerequisitesText = structureData.PrerequisitesText,
                CreatedAt = DateTimeOffset.UtcNow
            };

            structure = await _curriculumStructureRepository.AddAsync(structure, cancellationToken);
            _logger.LogInformation("Created new curriculum structure with ID: {StructureId}", structure.Id);
        }

        response.Message = "Curriculum imported successfully";
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