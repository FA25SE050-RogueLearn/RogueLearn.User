using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;
using RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubject;
using RogueLearn.User.Application.Features.Subjects.Commands.DeleteSubject;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;
using RogueLearn.User.Application.Features.Subjects.Queries.GetSubjectById;
using RogueLearn.User.Application.Features.SyllabusVersions.Queries.GetSyllabusVersionsBySubject;
using RogueLearn.User.Application.Features.SyllabusVersions.Commands.CreateSyllabusVersion;
using RogueLearn.User.Application.Features.SyllabusVersions.Commands.UpdateSyllabusVersion;
using RogueLearn.User.Application.Features.SyllabusVersions.Commands.DeleteSyllabusVersion;
using RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText; 

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/subjects")]
[AdminOnly]
public class SubjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Imports a single subject from raw text, creating or updating it in the master catalog.
    /// </summary>
    [HttpPost("import-from-text")] // NEW ENDPOINT
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CreateSubjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateSubjectResponse>> ImportFromText([FromForm] string rawText, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return BadRequest("The 'rawText' form field is required and cannot be empty.");
        }

        var command = new ImportSubjectFromTextCommand { RawText = rawText };
        var result = await _mediator.Send(command, cancellationToken);
        // Return 200 OK because it's an "upsert" operation which could be a create or update.
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<SubjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<SubjectDto>>> GetAllSubjects()
    {
        var query = new GetAllSubjectsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SubjectDto>> GetSubjectById(Guid id)
    {
        var query = new GetSubjectByIdQuery { Id = id };
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateSubjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateSubjectResponse>> CreateSubject([FromBody] CreateSubjectCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetSubjectById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UpdateSubjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UpdateSubjectResponse>> UpdateSubject(Guid id, [FromBody] UpdateSubjectCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteSubject(Guid id)
    {
        var command = new DeleteSubjectCommand { Id = id };
        await _mediator.Send(command);
        return NoContent();
    }
}