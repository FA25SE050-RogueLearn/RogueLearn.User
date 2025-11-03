// RogueLearn.User/src/RogueLearn.User.Api/Controllers/SyllabusVersionsController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.SyllabusVersions.Queries.GetSyllabusVersionsBySubject;
using RogueLearn.User.Application.Features.SyllabusVersions.Commands.CreateSyllabusVersion;
using RogueLearn.User.Application.Features.SyllabusVersions.Commands.UpdateSyllabusVersion;
using RogueLearn.User.Application.Features.SyllabusVersions.Commands.DeleteSyllabusVersion;
using RogueLearn.User.Domain.Interfaces; // ADDED: To directly access the repository.
using System.Text.Json; // ADDED: To handle JSON serialization if needed.

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
    // ADDED: Inject the repository directly for our diagnostic endpoint.
    private readonly ISyllabusVersionRepository _syllabusVersionRepository;

    public SyllabusVersionsController(IMediator mediator, ISyllabusVersionRepository syllabusVersionRepository)
    {
        _mediator = mediator;
        // ADDED: Assign the injected repository.
        _syllabusVersionRepository = syllabusVersionRepository;
    }

    // ADDED: A new diagnostic endpoint to directly inspect the content the API is receiving.
    /// <summary>
    /// [DIAGNOSTIC] Gets the raw 'content' field for a specific syllabus version.
    /// </summary>
    /// <remarks>
    /// Use this endpoint to verify exactly what JSON content the application layer is receiving from the database for a given syllabus version ID.
    /// </remarks>
    /// <param name="id">The unique identifier of the syllabus version.</param>
    /// <returns>The raw JSONB content as a JSON object.</returns>
    [HttpGet("{id:guid}/content")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSyllabusContentById(Guid id)
    {
        var syllabusVersion = await _syllabusVersionRepository.GetByIdAsync(id, CancellationToken.None);
        if (syllabusVersion == null)
        {
            return NotFound(new { message = "Syllabus version not found." });
        }
        if (syllabusVersion.Content == null)
        {
            return NoContent();
        }
        // This will return the Dictionary<string, object> as a JSON object response.
        return Ok(syllabusVersion.Content);
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