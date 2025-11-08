using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Plugins;

namespace RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportCurriculum
{
    public class ImportCurriculumCommandHandler : IRequestHandler<ImportCurriculumCommand, ImportCurriculumResponse>
    {
        private readonly ICurriculumProgramRepository _curriculumProgramRepository;
        //private readonly ICurriculumVersionRepository _curriculumVersionRepository;
        private readonly ISubjectRepository _subjectRepository;
        // readonly ICurriculumStructureRepository _curriculumStructureRepository;
        private readonly ICurriculumImportStorage _storage;
        private readonly CurriculumImportDataValidator _validator;
        private readonly ILogger<ImportCurriculumCommandHandler> _logger;
        private readonly IFlmExtractionPlugin _flmPlugin;

        public ImportCurriculumCommandHandler(
            ICurriculumProgramRepository curriculumProgramRepository,
            //ICurriculumVersionRepository curriculumVersionRepository,
            ISubjectRepository subjectRepository,
           // ICurriculumStructureRepository curriculumStructureRepository,
            ICurriculumImportStorage curriculumImportStorage,
            CurriculumImportDataValidator validator,
            ILogger<ImportCurriculumCommandHandler> logger,
            IFlmExtractionPlugin flmPlugin)
        {
            _curriculumProgramRepository = curriculumProgramRepository;
            //_curriculumVersionRepository = curriculumVersionRepository;
            _subjectRepository = subjectRepository;
            //_curriculumStructureRepository = curriculumStructureRepository;
            _storage = curriculumImportStorage;
            _validator = validator;
            _logger = logger;
            _flmPlugin = flmPlugin;
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
                    // await SaveDataToStorageAsync(curriculumData, request.RawText, textHash, cancellationToken);
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
                var cachedJson = await _storage.TryGetByHashJsonAsync("curriculum-imports", textHash, cancellationToken);
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
                await _storage.SaveLatestAsync(
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
            return await _flmPlugin.ExtractCurriculumJsonAsync(rawText);
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
                    // Convert extracted double? to int? for domain entity
                    DurationYears = data.Program.DurationYears.HasValue
                        ? (int?)Convert.ToInt32(data.Program.DurationYears.Value)
                        : null,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                curriculumProgram = await _curriculumProgramRepository.AddAsync(curriculumProgram, cancellationToken);
                _logger.LogInformation("Created new curriculum program with ID {ProgramId}", curriculumProgram.Id);
            }
            else
            {
                curriculumProgram = existingProgram;
            }

            response.CurriculumProgramId = curriculumProgram.Id;

            // Check if curriculum version already exists
            //var existingVersion = await _curriculumVersionRepository.FirstOrDefaultAsync(
                //v => v.ProgramId == curriculumProgram.Id && v.VersionCode == data.Version.VersionCode, cancellationToken);

            //CurriculumVersion curriculumVersion;
            //if (existingVersion != null)
            {
                _logger.LogWarning("Curriculum version {VersionCode} for program {ProgramCode} already exists. Skipping import.", data.Version.VersionCode, data.Program.ProgramCode);
                return new ImportCurriculumResponse
                {
                    IsSuccess = false,
                    Message = $"Curriculum version {data.Version.VersionCode} for program {data.Program.ProgramCode} already exists. Import skipped to prevent duplicates."
                };
            }

            // Create curriculum version
            //curriculumVersion = new CurriculumVersion
            //{
             //   ProgramId = curriculumProgram.Id,
             //   VersionCode = data.Version.VersionCode,
             //   EffectiveYear = data.Version.EffectiveYear,
             //   IsActive = data.Version.IsActive,
             //   Description = data.Version.Description,
              //  CreatedAt = DateTimeOffset.UtcNow
            //};

            //curriculumVersion = await _curriculumVersionRepository.AddAsync(curriculumVersion, cancellationToken);
            //_logger.LogInformation("Created new curriculum version with ID {VersionId}", curriculumVersion.Id);
            //response.CurriculumVersionId = curriculumVersion.Id;

            // Bulk create/update subjects
            var subjectCodes = data.Subjects
                .Select(s => s.SubjectCode)
                .Union(data.Structure.SelectMany(st => st.PrerequisiteSubjectCodes ?? Enumerable.Empty<string>()))
                .Distinct()
                .ToList();
            
            // Fetch existing subjects by individual queries to avoid LINQ expression issues
            var existingSubjects = new List<Subject>();
            foreach (var code in subjectCodes)
            {
                var existing = await _subjectRepository.FirstOrDefaultAsync(s => s.SubjectCode == code, cancellationToken);
                if (existing != null)
                {
                    existingSubjects.Add(existing);
                }
            }
            var existingSubjectsByCode = existingSubjects.ToDictionary(s => s.SubjectCode, s => s);

            var subjectsToUpdate = new List<Subject>();
            var subjectsToInsert = new List<Subject>();

            foreach (var subjectData in data.Subjects)
            {
                if (existingSubjectsByCode.TryGetValue(subjectData.SubjectCode, out var existing))
                {
                    existing.SubjectName = subjectData.SubjectName;
                    existing.Credits = subjectData.Credits;
                    existing.Description = subjectData.Description;
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
                    subjectsToUpdate.Add(existing);
                }
                else
                {
                    subjectsToInsert.Add(new Subject
                    {
                        SubjectCode = subjectData.SubjectCode,
                        SubjectName = subjectData.SubjectName,
                        Credits = subjectData.Credits,
                        Description = subjectData.Description,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    });
                }
            }

            IEnumerable<Subject> updatedSubjects = Enumerable.Empty<Subject>();
            IEnumerable<Subject> insertedSubjects = Enumerable.Empty<Subject>();

            if (subjectsToUpdate.Any())
            {
                updatedSubjects = await _subjectRepository.UpdateRangeAsync(subjectsToUpdate, cancellationToken);
                _logger.LogInformation("Bulk updated {Count} subjects", subjectsToUpdate.Count);
            }

            if (subjectsToInsert.Any())
            {
                insertedSubjects = await _subjectRepository.AddRangeAsync(subjectsToInsert, cancellationToken);
                _logger.LogInformation("Bulk created {Count} subjects", subjectsToInsert.Count);
            }

            var allSubjects = updatedSubjects.Concat(insertedSubjects).ToList();
            response.SubjectIds.AddRange(allSubjects.Select(s => s.Id));

            // Build a lookup for subject code -> subject id for structures and prerequisites
            var subjectIdByCode = allSubjects.ToDictionary(s => s.SubjectCode, s => s.Id);
            // For any subjects that were already existing but weren't updated/inserted, ensure they are present in the lookup
            foreach (var kvp in existingSubjectsByCode)
            {
                if (!subjectIdByCode.ContainsKey(kvp.Key))
                {
                    subjectIdByCode[kvp.Key] = kvp.Value.Id;
                }
            }

            // Bulk create curriculum structures
           // var structuresToInsert = new List<Domain.Entities.CurriculumStructure>();
            foreach (var structureData in data.Structure)
            {
                if (!subjectIdByCode.TryGetValue(structureData.SubjectCode, out var subjectId))
                {
                    // Subject not found; skip this structure
                    continue;
                }

                // Map prerequisite subject codes to IDs
                Guid[]? prerequisiteIds = null;
                if (structureData.PrerequisiteSubjectCodes?.Any() == true)
                {
                    var ids = new List<Guid>();
                    foreach (var prereqCode in structureData.PrerequisiteSubjectCodes)
                    {
                        if (subjectIdByCode.TryGetValue(prereqCode, out var prereqId))
                        {
                            ids.Add(prereqId);
                        }
                    }
                    prerequisiteIds = ids.Any() ? ids.ToArray() : null;
                }

                //structuresToInsert.Add(new Domain.Entities.CurriculumStructure
                //{
                //    CurriculumVersionId = curriculumVersion.Id,
                //    SubjectId = subjectId,
                //    Semester = structureData.TermNumber,
                //    IsMandatory = structureData.IsMandatory,
                //    PrerequisiteSubjectIds = prerequisiteIds,
                //    PrerequisitesText = structureData.PrerequisitesText,
                //    CreatedAt = DateTimeOffset.UtcNow
                //});
            }

            //if (structuresToInsert.Any())
            //{
            //    var insertedStructures = await _curriculumStructureRepository.AddRangeAsync(structuresToInsert, cancellationToken);
            //    _logger.LogInformation("Bulk created {Count} curriculum structures", structuresToInsert.Count);
            //}

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
}
