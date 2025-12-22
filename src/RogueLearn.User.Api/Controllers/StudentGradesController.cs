using BuildingBlocks.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.Student.Commands.UpdateSingleSubjectGrade;
using System.ComponentModel.DataAnnotations;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/student/grades")]
[Authorize]
public class StudentGradesController : ControllerBase
{
    private readonly IMediator _mediator;

    public StudentGradesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Manually updates a single subject grade for the authenticated student.
    /// This triggers XP calculation and quest difficulty adjustments just like the bulk import.
    /// </summary>
    /// <param name="command">Update payload containing subject ID, grade, and status.</param>
    /// <returns>Result of the update operation.</returns>
    [HttpPut("subject")]
    [ProducesResponseType(typeof(UpdateSingleSubjectGradeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UpdateSingleSubjectGradeResponse>> UpdateSingleSubjectGrade(
        [FromBody] UpdateSingleSubjectGradeCommand command,
        CancellationToken cancellationToken)
    {
        command.AuthUserId = User.GetAuthUserId();
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}