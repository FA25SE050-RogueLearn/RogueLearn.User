using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text.Json;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateSyllabus;

public class ValidateSyllabusQueryHandler : IRequestHandler<ValidateSyllabusQuery, ValidateSyllabusResponse>
{
    private readonly Kernel _kernel;
    private readonly SyllabusDataValidator _validator;
    private readonly ILogger<ValidateSyllabusQueryHandler> _logger;

    public ValidateSyllabusQueryHandler(
        Kernel kernel,
        SyllabusDataValidator validator,
        ILogger<ValidateSyllabusQueryHandler> logger)
    {
        _kernel = kernel;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ValidateSyllabusResponse> Handle(ValidateSyllabusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting syllabus validation from text");

            // Step 1: Extract structured data using AI
            var extractedJson = await ExtractSyllabusDataAsync(request.RawText);
            if (string.IsNullOrEmpty(extractedJson))
            {
                return new ValidateSyllabusResponse
                {
                    IsValid = false,
                    Message = "Failed to extract syllabus data from the provided text"
                };
            }

            // Step 2: Parse JSON
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

            // Step 3: Validate extracted data
            var validationResult = await _validator.ValidateAsync(syllabusData, cancellationToken);
            
            var response = new ValidateSyllabusResponse
            {
                IsValid = validationResult.IsValid,
                ExtractedData = syllabusData,
                ValidationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
            };

            if (validationResult.IsValid)
            {
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
}