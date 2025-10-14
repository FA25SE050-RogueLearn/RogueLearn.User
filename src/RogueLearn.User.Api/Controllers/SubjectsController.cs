using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;
using RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubject;
using RogueLearn.User.Application.Features.Subjects.Commands.DeleteSubject;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;
using RogueLearn.User.Application.Features.Subjects.Queries.GetSubjectById;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/subjects")]
[AdminOnly]
/// <summary>
/// Admin endpoints for managing subjects (CRUD operations).
/// </summary>
/// <remarks>
/// Access restricted via <see cref="AdminOnlyAttribute"/>.
/// </remarks>
public class SubjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all subjects.
    /// </summary>
    /// <remarks>
    /// GET <c>api/admin/subjects</c>
    /// </remarks>
    /// <returns>List of all subjects.</returns>
    [HttpGet]
    public async Task<ActionResult<List<SubjectDto>>> GetAllSubjects()
    {
        var query = new GetAllSubjectsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets a subject by ID.
    /// </summary>
    /// <remarks>
    /// GET <c>api/admin/subjects/{id}</c>
    /// </remarks>
    /// <param name="id">Subject unique identifier.</param>
    /// <returns>Subject details.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SubjectDto>> GetSubjectById(Guid id)
    {
        var query = new GetSubjectByIdQuery { Id = id };
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }

    /// <summary>
    /// Creates a new subject.
    /// </summary>
    /// <remarks>
    /// POST <c>api/admin/subjects</c>
    /// </remarks>
    /// <param name="command">Subject creation payload.</param>
    /// <returns>Created subject details.</returns>
    [HttpPost]
    public async Task<ActionResult<CreateSubjectResponse>> CreateSubject([FromBody] CreateSubjectCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetSubjectById), new { id = result.Id }, result); 
    }

    /// <summary>
    /// Updates an existing subject.
    /// </summary>
    /// <remarks>
    /// PUT <c>api/admin/subjects/{id}</c>
    /// </remarks>
    /// <param name="id">Subject unique identifier.</param>
    /// <param name="command">Subject update payload.</param>
    /// <returns>Updated subject details.</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UpdateSubjectResponse>> UpdateSubject(Guid id, [FromBody] UpdateSubjectCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a subject.
    /// </summary>
    /// <remarks>
    /// DELETE <c>api/admin/subjects/{id}</c>
    /// </remarks>
    /// <param name="id">Subject unique identifier.</param>
    /// <returns>Status message confirming deletion.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSubject(Guid id)
    {
        var command = new DeleteSubjectCommand { Id = id };
        await _mediator.Send(command);
        return Ok(new { message = "Subject deleted successfully" });
    }
}