// RogueLearn.User/src/RogueLearn.User.Api/Controllers/CurriculumProgramSubjectsController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.CurriculumProgramSubjects.Commands.AddSubjectToProgram;
using RogueLearn.User.Application.Features.CurriculumProgramSubjects.Commands.RemoveSubjectFromProgram;
using RogueLearn.User.Application.Features.CurriculumProgramSubjects.Queries.GetSubjectsByProgram;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/programs/{programId:guid}/subjects")]
[AdminOnly]
public class CurriculumProgramSubjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CurriculumProgramSubjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all subjects linked to the specified curriculum program.
    /// </summary>
    /// <param name="programId">The ID of the curriculum program.</param>
    /// <returns>A list of subject DTOs.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<SubjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSubjectsByProgram(Guid programId)
    {
        var query = new GetSubjectsByProgramQuery { ProgramId = programId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Adds an existing subject to a curriculum program.
    /// </summary>
    /// <param name="programId">The ID of the curriculum program.</param>
    /// <param name="request">The request body containing the subject ID to add.</param>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddSubjectToProgram(Guid programId, [FromBody] AddSubjectToProgramRequest request)
    {
        var command = new AddSubjectToProgramCommand
        {
            ProgramId = programId,
            SubjectId = request.SubjectId
        };
        var result = await _mediator.Send(command);

        // Return a 201 Created with a location header pointing to the new resource relationship.
        // In this case, it's conceptually the subject within the context of the program.
        return CreatedAtAction(nameof(CurriculumProgramsController.GetById), "CurriculumPrograms", new { id = programId }, result);
    }

    /// <summary>
    /// Removes a subject from a curriculum program.
    /// </summary>
    /// <param name="programId">The ID of the curriculum program.</param>
    /// <param name="subjectId">The ID of the subject to remove.</param>
    [HttpDelete("{subjectId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSubjectFromProgram(Guid programId, Guid subjectId)
    {
        var command = new RemoveSubjectFromProgramCommand
        {
            ProgramId = programId,
            SubjectId = subjectId
        };
        await _mediator.Send(command);
        return NoContent();
    }
}