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
    ""programCode"": ""string (max 50 chars)"",
    ""programName"": ""string (max 255 chars)"",
    ""description"": ""string (optional)"",
    ""degreeLevel"": 1,
    ""totalCredits"": number (optional),
    ""durationYears"": number (optional)
  }},
  ""version"": {{
    ""versionCode"": ""string (max 50 chars, e.g., 'V1.0', '2022')"",
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
- degreeLevel: Use 1 for Bachelor's, 2 for Master's, 3 for Doctoral
- effectiveYear: Extract year from any date mentioned (e.g., from ""2022-10-26"" use 2022)
- versionCode: Generate a meaningful version code if not explicitly mentioned
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
            
            // Clean up the response - remove markdown code blocks if present
            var cleanedResponse = rawResponse.Trim();
            if (cleanedResponse.StartsWith("```json"))
            {
                cleanedResponse = cleanedResponse.Substring(7); // Remove ```json
            }
            else if (cleanedResponse.StartsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(3); // Remove ```
            }
            
            if (cleanedResponse.EndsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3); // Remove trailing ```
            }
            
            return cleanedResponse.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract curriculum data using AI");
            return string.Empty;
        }
    }
}