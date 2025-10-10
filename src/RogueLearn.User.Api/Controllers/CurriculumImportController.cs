using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportCurriculum;
using RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportSyllabus;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateSyllabus;

namespace RogueLearn.User.Api.Controllers;

/// <summary>
/// Controller responsible for handling curriculum and syllabus import operations.
/// Provides endpoints for importing and validating curriculum and syllabus data from raw text.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AdminOnly] // Restricts access to admin users only
public class CurriculumImportController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the CurriculumImportController.
    /// </summary>
    /// <param name="mediator">The MediatR instance for handling commands and queries</param>
    public CurriculumImportController(IMediator mediator)
    {
        _mediator = mediator;
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
}