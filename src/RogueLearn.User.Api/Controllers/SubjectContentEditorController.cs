using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubjectContent;
using RogueLearn.User.Application.Features.Subjects.Queries.GetSubjectContent;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/subjects/{subjectId:guid}")]
[AdminOnly]
public class SubjectContentEditorController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubjectContentEditorController> _logger;

    public SubjectContentEditorController(
        IMediator mediator,
        ILogger<SubjectContentEditorController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets the JSON content (syllabus) of a subject.
    /// Used by the admin UI to display and edit subject content.
    /// </summary>
    [HttpGet("content")]
    [ProducesResponseType(typeof(SyllabusContent), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubjectContent(
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetSubjectContentQuery { SubjectId = subjectId };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subject content");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates the JSON content (syllabus) of a subject.
    /// Replaces the entire SyllabusContent.
    /// </summary>
    [HttpPut("content")]
    [ProducesResponseType(typeof(SyllabusContent), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSubjectContent(
        Guid subjectId,
        [FromBody] SyllabusContent content,
        CancellationToken cancellationToken)
    {
        if (content == null)
        {
            return BadRequest(new { error = "Content cannot be null" });
        }

        try
        {
            var command = new UpdateSubjectContentCommand
            {
                SubjectId = subjectId,
                Content = content
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subject content");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}
