using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportCurriculum;
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

    public CurriculumImportController(IMediator mediator, ICurriculumImportStorage curriculumImportStorage)
    {
        _mediator = mediator;
        _curriculumImportStorage = curriculumImportStorage;
    }

    /// <summary>
    /// Imports curriculum data from raw text and processes it into structured curriculum entities.
    /// </summary>
    /// <param name="rawText">The import request containing raw curriculum text.</param>
    /// <returns>Success response with imported curriculum data or error response with validation details</returns>
    /// <response code="200">Curriculum imported successfully</response>
    /// <response code="400">Invalid request data or import validation failed</response>
    [HttpPost("curriculum")]
    [Consumes("multipart/form-data")] // MODIFIED: Specifies the expected content type.
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ImportCurriculum([FromForm] string rawText) // MODIFIED: Changed from [FromBody] to [FromForm] and simplified signature.
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return BadRequest("Raw text is required");
        }

        // MODIFIED: Manually construct the command from the form data.
        var command = new ImportCurriculumCommand { RawText = rawText };

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

  

    /// <summary>
    /// Validates curriculum data from raw text without importing it.
    /// </summary>
    /// <param name="rawText">The validation query containing raw curriculum text.</param>
    /// <returns>Validation result with extracted data and any validation errors</returns>
    /// <response code="200">Validation completed (may contain validation errors in response)</response>
    /// <response code="400">Invalid request data</response>
    [HttpPost("curriculum/validate")]
    [AllowAnonymous] // TEMP: Allow local testing without auth
    [Consumes("multipart/form-data")] // MODIFIED: Specifies the expected content type.
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ValidateCurriculum([FromForm] string rawText) // MODIFIED: Changed from [FromBody] to [FromForm].
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return BadRequest("Raw text is required");
        }

        // MODIFIED: Manually construct the query.
        var query = new ValidateCurriculumQuery { RawText = rawText };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Validates syllabus data from raw text without importing it.
    /// </summary>
    /// <param name="rawText">The validation query containing raw syllabus text.</param>
    /// <returns>Validation result with extracted data and any validation errors</returns>
    /// <response code="200">Validation completed (may contain validation errors in response)</response>
    /// <response code="400">Invalid request data</response>
    [HttpPost("syllabus/validate")]
    [AllowAnonymous] // TEMP: Allow local testing without auth
    [Consumes("multipart/form-data")] // MODIFIED: Specifies the expected content type.
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ValidateSyllabus([FromForm] string rawText) // MODIFIED: Changed from [FromBody] to [FromForm].
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return BadRequest("Raw text is required");
        }

        // MODIFIED: Manually construct the query.
        var query = new ValidateSyllabusQuery { RawText = rawText };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Clears cached curriculum data by hash to force re-processing.
    /// </summary>
    [HttpDelete("curriculum/cache/{rawTextHash}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClearCurriculumCache(string rawTextHash)
    {
        if (string.IsNullOrWhiteSpace(rawTextHash))
        {
            return BadRequest("Raw text hash is required");
        }

        var success = await _curriculumImportStorage.ClearCacheByHashAsync("curriculum-imports", rawTextHash);

        return NoContent();
    }

    /// <summary>
    /// Clears all cached curriculum data for a specific program and version.
    /// </summary>
    [HttpDelete("curriculum/cache/{programCode}/{versionCode}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClearProgramVersionCache(string programCode, string versionCode)
    {
        if (string.IsNullOrWhiteSpace(programCode) || string.IsNullOrWhiteSpace(versionCode))
        {
            return BadRequest("Program code and version code are required");
        }

        var success = await _curriculumImportStorage.ClearCacheForProgramVersionAsync("curriculum-imports", programCode, versionCode);

        return NoContent();
    }
}