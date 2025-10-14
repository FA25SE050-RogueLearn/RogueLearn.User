using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.SyllabusVersions.Queries.GetSyllabusVersionsBySubject;
using RogueLearn.User.Application.Features.SyllabusVersions.Commands.CreateSyllabusVersion;
using RogueLearn.User.Application.Features.SyllabusVersions.Commands.UpdateSyllabusVersion;
using RogueLearn.User.Application.Features.SyllabusVersions.Commands.DeleteSyllabusVersion;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/subjects/{subjectId:guid}/syllabus-versions")]
[AdminOnly]
/// <summary>
/// Admin endpoints for managing syllabus versions within a subject.
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
    /// GET <c>api/admin/subjects/{subjectId}/syllabus-versions</c>
    /// </remarks>
    /// <param name="subjectId">Subject unique identifier.</param>
    /// <returns>List of syllabus versions for the subject.</returns>
    [HttpGet]
    public async Task<ActionResult<List<SyllabusVersionDto>>> GetSyllabusVersionsBySubject(Guid subjectId)
    {
        var query = new GetSyllabusVersionsBySubjectQuery(subjectId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new syllabus version.
    /// </summary>
    /// <remarks>
    /// POST <c>api/admin/subjects/{subjectId}/syllabus-versions</c>
    /// </remarks>
    /// <param name="subjectId">Subject unique identifier.</param>
    /// <param name="command">Create syllabus version payload.</param>
    /// <returns>Created syllabus version.</returns>
    [HttpPost]
    public async Task<ActionResult<CreateSyllabusVersionResponse>> CreateSyllabusVersion(Guid subjectId, [FromBody] CreateSyllabusVersionCommand command)
    {
        command.SubjectId = subjectId;
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetSyllabusVersionsBySubject), new { subjectId }, result);
    }

    /// <summary>
    /// Updates an existing syllabus version.
    /// </summary>
    /// <remarks>
    /// PUT <c>api/admin/subjects/{subjectId}/syllabus-versions/{id}</c>
    /// </remarks>
    /// <param name="subjectId">Subject unique identifier.</param>
    /// <param name="id">Syllabus version unique identifier.</param>
    /// <param name="command">Update syllabus version payload.</param>
    /// <returns>Updated syllabus version.</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UpdateSyllabusVersionResponse>> UpdateSyllabusVersion(Guid subjectId, Guid id, [FromBody] UpdateSyllabusVersionCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a syllabus version.
    /// </summary>
    /// <remarks>
    /// DELETE <c>api/admin/subjects/{subjectId}/syllabus-versions/{id}</c>
    /// </remarks>
    /// <param name="subjectId">Subject unique identifier.</param>
    /// <param name="id">Syllabus version unique identifier.</param>
    /// <returns>Status message confirming deletion.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSyllabusVersion(Guid subjectId, Guid id)
    {
        var command = new DeleteSyllabusVersionCommand(id);
        await _mediator.Send(command);
        return Ok(new { message = "Syllabus version deleted successfully" });
    }
}