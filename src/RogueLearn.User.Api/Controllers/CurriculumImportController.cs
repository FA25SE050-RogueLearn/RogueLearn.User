// RogueLearn.User/src/RogueLearn.User.Api/Controllers/CurriculumImportController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportCurriculum;
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

    public CurriculumImportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Imports curriculum data from raw text and processes it into structured curriculum entities.
    /// </summary>
    /// <param name="rawText">The import request containing raw curriculum text.</param>
    /// <returns>Success response with imported curriculum data or error response with validation details</returns>
    /// <response code="200">Curriculum imported successfully</response>
    /// <response code="400">Invalid request data or import validation failed</response>
    [HttpPost("curriculum")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ImportCurriculum([FromForm] string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return BadRequest("Raw text is required");
        }

        var command = new ImportCurriculumCommand { RawText = rawText };

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }
}