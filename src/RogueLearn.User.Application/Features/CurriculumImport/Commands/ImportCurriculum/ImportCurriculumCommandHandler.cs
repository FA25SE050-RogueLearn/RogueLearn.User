using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text.Json;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;

namespace RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportCurriculum;

public class ImportCurriculumCommandHandler : IRequestHandler<ImportCurriculumCommand, ImportCurriculumResponse>
{
    private readonly Kernel _kernel;
    private readonly ICurriculumProgramRepository _curriculumProgramRepository;
    private readonly ICurriculumVersionRepository _curriculumVersionRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly CurriculumImportDataValidator _validator;
    private readonly ILogger<ImportCurriculumCommandHandler> _logger;

    public ImportCurriculumCommandHandler(
        Kernel kernel,
        ICurriculumProgramRepository curriculumProgramRepository,
        ICurriculumVersionRepository curriculumVersionRepository,
        ISubjectRepository subjectRepository,
        ICurriculumStructureRepository curriculumStructureRepository,
        CurriculumImportDataValidator validator,
        ILogger<ImportCurriculumCommandHandler> logger)
    {
        _kernel = kernel;
        _curriculumProgramRepository = curriculumProgramRepository;
        _curriculumVersionRepository = curriculumVersionRepository;
        _subjectRepository = subjectRepository;
        _curriculumStructureRepository = curriculumStructureRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ImportCurriculumResponse> Handle(ImportCurriculumCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting curriculum import from text");

            // Step 1: Extract structured data using AI
            var extractedJson = await ExtractCurriculumDataAsync(request.RawText);
            if (string.IsNullOrEmpty(extractedJson))
            {
                return new ImportCurriculumResponse
                {
                    IsSuccess = false,
                    Message = "Failed to extract curriculum data from the provided text"
                };
            }

            // Step 2: Parse JSON
            CurriculumImportData? curriculumData;
            try
            {
                curriculumData = JsonSerializer.Deserialize<CurriculumImportData>(extractedJson);
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

            // Step 3: Validate extracted data
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

            // Step 4: Map and persist data
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

    private async Task<string> ExtractCurriculumDataAsync(string rawText)
    {
        var prompt = $@"
Extract curriculum information from the following text and return it as JSON following this exact schema:

{{
  ""program"": {{
    ""programCode"": ""string (max 20 chars)"",
    ""programName"": ""string (max 200 chars)"",
    ""description"": ""string (optional)""
  }},
  ""version"": {{
    ""versionNumber"": number,
    ""effectiveDate"": ""YYYY-MM-DD"",
    ""description"": ""string (optional)""
  }},
  ""subjects"": [
    {{
      ""subjectCode"": ""string (max 20 chars)"",
      ""subjectName"": ""string (max 200 chars)"",
      ""credits"": number,
      ""description"": ""string (optional)""
    }}
  ],
  ""structure"": [
    {{
      ""subjectCode"": ""string"",
      ""termNumber"": number,
      ""isMandatory"": boolean,
      ""prerequisiteSubjectCodes"": [""string""],
      ""prerequisitesText"": ""string (optional)""
    }}
  ]
}}

Text to extract from:
{rawText}

Return only the JSON, no additional text or formatting.";

        try
        {
            var result = await _kernel.InvokePromptAsync(prompt);
            return result.GetValue<string>() ?? string.Empty;
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
        Domain.Entities.CurriculumProgram curriculumProgram;

        if (existingProgram == null)
        {
            curriculumProgram = new Domain.Entities.CurriculumProgram
            {
                Id = Guid.NewGuid(),
                ProgramName = data.Program.ProgramName,
                ProgramCode = data.Program.ProgramCode,
                Description = data.Program.Description,
                DegreeLevel = data.Program.DegreeLevel,
                TotalCredits = data.Program.TotalCredits,
                DurationYears = data.Program.DurationYears,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await _curriculumProgramRepository.AddAsync(curriculumProgram, cancellationToken);
        }
        else
        {
            curriculumProgram = existingProgram;
        }

        response.CurriculumProgramId = curriculumProgram.Id;

        // Create curriculum version
        var curriculumVersion = new Domain.Entities.CurriculumVersion
        {
            Id = Guid.NewGuid(),
            ProgramId = curriculumProgram.Id,
            VersionCode = data.Version.VersionCode,
            EffectiveYear = data.Version.EffectiveYear,
            IsActive = data.Version.IsActive,
            Description = data.Version.Description,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _curriculumVersionRepository.AddAsync(curriculumVersion, cancellationToken);

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
            }
            else
            {
                subject = new Subject
                {
                    Id = Guid.NewGuid(),
                    SubjectCode = subjectData.SubjectCode,
                    SubjectName = subjectData.SubjectName,
                    Credits = subjectData.Credits,
                    Description = subjectData.Description,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                await _subjectRepository.AddAsync(subject, cancellationToken);
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
                Id = Guid.NewGuid(),
                CurriculumVersionId = curriculumVersion.Id,
                SubjectId = subject.Id,
                TermNumber = structureData.TermNumber,
                IsMandatory = structureData.IsMandatory,
                PrerequisiteSubjectIds = prerequisiteIds.ToArray(),
                PrerequisitesText = structureData.PrerequisitesText,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _curriculumStructureRepository.AddAsync(structure, cancellationToken);
        }

        response.Message = "Curriculum imported successfully";
        return response;
    }
}