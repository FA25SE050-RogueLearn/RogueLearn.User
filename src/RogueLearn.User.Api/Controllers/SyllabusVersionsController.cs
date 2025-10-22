using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.SyllabusVersions.Queries.GetSyllabusVersionsBySubject;
using RogueLearn.User.Application.Features.SyllabusVersions.Commands.CreateSyllabusVersion;
using RogueLearn.User.Application.Features.SyllabusVersions.Commands.UpdateSyllabusVersion;
using RogueLearn.User.Application.Features.SyllabusVersions.Commands.DeleteSyllabusVersion;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/syllabus-versions")]
[AdminOnly]
/// <summary>
/// Admin endpoints for managing syllabus versions.
/// </summary>
/// <remarks>
/// Access restricted via <see cref="AdminOnlyAttribute"/>.
/// </remarks>
public class SyllabusVersionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SyllabusVersionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets syllabus versions by subject ID.
    /// </summary>
    /// <remarks>
    /// GET <c>api/admin/syllabus-versions/subject/{subjectId}</c>
    /// </remarks>
    /// <param name="subjectId">Subject unique identifier.</param>
    /// <returns>List of syllabus versions for the subject.</returns>
    [HttpGet("subject/{subjectId:guid}")]
    [ProducesResponseType(typeof(List<SyllabusVersionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<SyllabusVersionDto>>> GetBySubject(Guid subjectId)
    {
        var query = new GetSyllabusVersionsBySubjectQuery(subjectId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new syllabus version.
    /// </summary>
    /// <remarks>
    /// POST <c>api/admin/syllabus-versions</c>
    /// </remarks>
    /// <param name="command">Create syllabus version payload.</param>
    /// <returns>Created syllabus version.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateSyllabusVersionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateSyllabusVersionResponse>> Create([FromBody] CreateSyllabusVersionCommand command)
    {
        var result = await _mediator.Send(command);
        return Created(string.Empty, result);
    }

    /// <summary>
    /// Updates an existing syllabus version.
    /// </summary>
    /// <remarks>
    /// PUT <c>api/admin/syllabus-versions/{id}</c>
    /// </remarks>
    /// <param name="id">Syllabus version unique identifier.</param>
    /// <param name="command">Update syllabus version payload.</param>
    /// <returns>Updated syllabus version.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UpdateSyllabusVersionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UpdateSyllabusVersionResponse>> Update(Guid id, [FromBody] UpdateSyllabusVersionCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a syllabus version.
    /// </summary>
    /// <remarks>
    /// DELETE <c>api/admin/syllabus-versions/{id}</c>
    /// </remarks>
    /// <param name="id">Syllabus version unique identifier.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteSyllabusVersionCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }
}