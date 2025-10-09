using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text.Json;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;

public class ValidateCurriculumQueryHandler : IRequestHandler<ValidateCurriculumQuery, ValidateCurriculumResponse>
{
    private readonly Kernel _kernel;
    private readonly CurriculumImportDataValidator _validator;
    private readonly ILogger<ValidateCurriculumQueryHandler> _logger;

    public ValidateCurriculumQueryHandler(
        Kernel kernel,
        CurriculumImportDataValidator validator,
        ILogger<ValidateCurriculumQueryHandler> logger)
    {
        _kernel = kernel;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ValidateCurriculumResponse> Handle(ValidateCurriculumQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting curriculum validation from text");

            // Step 1: Extract structured data using AI
            var extractedJson = await ExtractCurriculumDataAsync(request.RawText);
            if (string.IsNullOrEmpty(extractedJson))
            {
                return new ValidateCurriculumResponse
                {
                    IsValid = false,
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

            // Step 3: Validate extracted data
            var validationResult = await _validator.ValidateAsync(curriculumData, cancellationToken);
            
            var response = new ValidateCurriculumResponse
            {
                IsValid = validationResult.IsValid,
                ExtractedData = curriculumData,
                ValidationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
            };

            if (validationResult.IsValid)
            {
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
}