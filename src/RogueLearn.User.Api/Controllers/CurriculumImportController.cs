using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportCurriculum;
using RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportSyllabus;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateSyllabus;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Api.Controllers;

/// <summary>
/// Controller responsible for handling curriculum and syllabus import operations.
/// Provides endpoints for importing and validating curriculum and syllabus data from raw text.
/// </summary>
[ApiController]
[Route("api/admin")]
[AdminOnly] // Restricts access to admin users only
public class CurriculumImportController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurriculumImportStorage _curriculumImportStorage;

    /// <summary>
    /// Initializes a new instance of the CurriculumImportController.
    /// </summary>
    /// <param name="mediator">The MediatR instance for handling commands and queries</param>
    /// <param name="curriculumImportStorage">The curriculum import storage service</param>
    public CurriculumImportController(IMediator mediator, ICurriculumImportStorage curriculumImportStorage)
    {
        _mediator = mediator;
        _curriculumImportStorage = curriculumImportStorage;
    }

    /// <summary>
    /// Imports curriculum data from raw text and processes it into structured curriculum entities.
    /// </summary>
    /// <param name="command">The import request containing raw curriculum text and optional creator ID</param>
    /// <returns>Success response with imported curriculum data or error response with validation details</returns>
    /// <response code="200">Curriculum imported successfully</response>
    /// <response code="400">Invalid request data or import validation failed</response>
    [HttpPost("curriculum")]
    public async Task<IActionResult> ImportCurriculum([FromBody] ImportCurriculumCommand command)
    {
        // Validate that raw text is provided
        if (string.IsNullOrWhiteSpace(command.RawText))
        {
            return BadRequest("Raw text is required");
        }

        // Process the import command through MediatR
        var result = await _mediator.Send(command);
        
        // Return appropriate response based on result
        if (result.IsSuccess)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Imports syllabus data from raw text and processes it into structured syllabus entities.
    /// </summary>
    /// <param name="command">The import command containing raw syllabus text and optional creator ID</param>
    /// <returns>Success response with imported syllabus data or error response with validation details</returns>
    /// <response code="200">Syllabus imported successfully</response>
    /// <response code="400">Invalid request data or import validation failed</response>
    [HttpPost("syllabus")]
    public async Task<IActionResult> ImportSyllabus([FromBody] ImportSyllabusCommand command)
    {
        // Validate that raw text is provided
        if (string.IsNullOrWhiteSpace(command.RawText))
        {
            return BadRequest("Raw text is required");
        }

        // Process the import command through MediatR
        var result = await _mediator.Send(command);
        
        // Return appropriate response based on result
        if (result.IsSuccess)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Validates curriculum data from raw text without importing it.
    /// Useful for checking data quality and structure before actual import.
    /// </summary>
    /// <param name="query">The validation query containing raw curriculum text</param>
    /// <returns>Validation result with extracted data and any validation errors</returns>
    /// <response code="200">Validation completed (may contain validation errors in response)</response>
    /// <response code="400">Invalid request data</response>
    [HttpPost("curriculum/validate")]
    public async Task<IActionResult> ValidateCurriculum([FromBody] ValidateCurriculumQuery query)
    {
        // Validate that raw text is provided
        if (string.IsNullOrWhiteSpace(query.RawText))
        {
            return BadRequest("Raw text is required");
        }

        // Process the validation query through MediatR
        var result = await _mediator.Send(query);
        return Ok(result); // Always return OK as validation results are in the response body
    }

    /// <summary>
    /// Validates syllabus data from raw text without importing it.
    /// Useful for checking data quality and structure before actual import.
    /// </summary>
    /// <param name="query">The validation query containing raw syllabus text</param>
    /// <returns>Validation result with extracted data and any validation errors</returns>
    /// <response code="200">Validation completed (may contain validation errors in response)</response>
    /// <response code="400">Invalid request data</response>
    [HttpPost("syllabus/validate")]
    public async Task<IActionResult> ValidateSyllabus([FromBody] ValidateSyllabusQuery query)
    {
        // Validate that raw text is provided
        if (string.IsNullOrWhiteSpace(query.RawText))
        {
            return BadRequest("Raw text is required");
        }

        // Process the validation query through MediatR
        var result = await _mediator.Send(query);
        return Ok(result); // Always return OK as validation results are in the response body
    }

    /// <summary>
    /// Clears cached curriculum data by hash to force re-processing.
    /// Useful for clearing old cached data with incorrect format.
    /// </summary>
    /// <param name="rawTextHash">The hash of the raw text to clear from cache</param>
    /// <returns>Success response indicating if cache was cleared</returns>
    /// <response code="200">Cache cleared successfully</response>
    /// <response code="400">Invalid hash provided</response>
    [HttpDelete("curriculum/cache/{rawTextHash}")]
    public async Task<IActionResult> ClearCurriculumCache(string rawTextHash)
    {
        if (string.IsNullOrWhiteSpace(rawTextHash))
        {
            return BadRequest("Raw text hash is required");
        }

        var success = await _curriculumImportStorage.ClearCacheByHashAsync("curriculum-imports", rawTextHash);
        
        return Ok(new { Success = success, Message = success ? "Cache cleared successfully" : "Failed to clear cache or cache not found" });
    }

    /// <summary>
    /// Clears all cached curriculum data for a specific program and version.
    /// </summary>
    /// <param name="programCode">The program code</param>
    /// <param name="versionCode">The version code</param>
    /// <returns>Success response indicating if cache was cleared</returns>
    /// <response code="200">Cache cleared successfully</response>
    /// <response code="400">Invalid parameters provided</response>
    [HttpDelete("curriculum/cache/{programCode}/{versionCode}")]
    public async Task<IActionResult> ClearProgramVersionCache(string programCode, string versionCode)
    {
        if (string.IsNullOrWhiteSpace(programCode) || string.IsNullOrWhiteSpace(versionCode))
        {
            return BadRequest("Program code and version code are required");
        }

        var success = await _curriculumImportStorage.ClearCacheForProgramVersionAsync("curriculum-imports", programCode, versionCode);
        
        return Ok(new { Success = success, Message = success ? "Cache cleared successfully" : "Failed to clear cache or cache not found" });
    }
}