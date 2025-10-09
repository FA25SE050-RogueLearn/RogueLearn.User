using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text.Json;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateSyllabus;

namespace RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportSyllabus;

public class ImportSyllabusCommandHandler : IRequestHandler<ImportSyllabusCommand, ImportSyllabusResponse>
{
    private readonly Kernel _kernel;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ISyllabusVersionRepository _syllabusVersionRepository;
    private readonly SyllabusDataValidator _validator;
    private readonly ILogger<ImportSyllabusCommandHandler> _logger;

    public ImportSyllabusCommandHandler(
        Kernel kernel,
        ISubjectRepository subjectRepository,
        ISyllabusVersionRepository syllabusVersionRepository,
        SyllabusDataValidator validator,
        ILogger<ImportSyllabusCommandHandler> logger)
    {
        _kernel = kernel;
        _subjectRepository = subjectRepository;
        _syllabusVersionRepository = syllabusVersionRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ImportSyllabusResponse> Handle(ImportSyllabusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting syllabus import from text");

            // Step 1: Extract structured data using AI
            var extractedJson = await ExtractSyllabusDataAsync(request.RawText);
            if (string.IsNullOrEmpty(extractedJson))
            {
                return new ImportSyllabusResponse
                {
                    IsSuccess = false,
                    Message = "Failed to extract syllabus data from the provided text"
                };
            }

            // Step 2: Parse JSON
            SyllabusData? syllabusData;
            try
            {
                syllabusData = JsonSerializer.Deserialize<SyllabusData>(extractedJson);
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

            // Step 3: Validate extracted data
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

            // Step 4: Map and persist data
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
        var prompt = $@"
Extract syllabus information from the following text and return it as JSON following this exact schema:

{{
  ""subject"": {{
    ""subjectCode"": ""string (max 20 chars)"",
    ""subjectName"": ""string (max 200 chars)"",
    ""credits"": number,
    ""description"": ""string (optional)""
  }},
  ""versionNumber"": number,
  ""effectiveDate"": ""YYYY-MM-DD"",
  ""content"": {{
    ""objectives"": [""string""],
    ""learningOutcomes"": [""string""],
    ""weeks"": [
      {{
        ""weekNumber"": number,
        ""topics"": [""string""],
        ""activities"": [""string""],
        ""materials"": [""string""]
      }}
    ],
    ""assessments"": [
      {{
        ""type"": ""string"",
        ""description"": ""string"",
        ""weight"": number,
        ""dueWeek"": number
      }}
    ],
    ""references"": [""string""],
    ""prerequisites"": ""string (optional)""
  }}
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
}