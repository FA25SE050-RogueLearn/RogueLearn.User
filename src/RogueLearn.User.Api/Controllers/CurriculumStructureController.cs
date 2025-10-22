using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.CurriculumStructure.Queries.GetCurriculumStructureByVersion;
using RogueLearn.User.Application.Features.CurriculumStructure.Commands.AddSubjectToCurriculum;
using RogueLearn.User.Application.Features.CurriculumStructure.Commands.UpdateCurriculumStructure;
using RogueLearn.User.Application.Features.CurriculumStructure.Commands.RemoveSubjectFromCurriculum;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/curriculum-structure")]
[AdminOnly]
/// <summary>
/// Admin endpoints for managing curriculum structure (subjects within curriculum versions).
/// </summary>
/// <remarks>
/// Access restricted via <see cref="AdminOnlyAttribute"/>.
/// </remarks>
public class CurriculumStructureController : ControllerBase
{
    private readonly IMediator _mediator;

    public CurriculumStructureController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets curriculum structure by version ID.
    /// </summary>
    /// <remarks>
    /// GET <c>api/admin/curriculum-structure/version/{versionId}</c>
    /// </remarks>
    /// <param name="versionId">Curriculum version unique identifier.</param>
    /// <returns>List of subjects in the curriculum structure.</returns>
    [HttpGet("version/{versionId:guid}")]
    [ProducesResponseType(typeof(List<CurriculumStructureDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<CurriculumStructureDto>>> GetCurriculumStructureByVersion(Guid versionId)
    {
        var query = new GetCurriculumStructureByVersionQuery(versionId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Adds a subject to curriculum structure.
    /// </summary>
    /// <remarks>
    /// POST <c>api/admin/curriculum-structure</c>
    /// </remarks>
    /// <param name="command">Add subject to curriculum payload.</param>
    /// <returns>Created curriculum structure entry.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AddSubjectToCurriculumResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AddSubjectToCurriculumResponse>> AddSubject([FromBody] AddSubjectToCurriculumCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetCurriculumStructureByVersion), new { versionId = result.CurriculumVersionId }, result);
    }

    /// <summary>
    /// Updates curriculum structure entry.
    /// </summary>
    /// <remarks>
    /// PUT <c>api/admin/curriculum-structure/{id}</c>
    /// </remarks>
    /// <param name="id">Curriculum structure entry unique identifier.</param>
    /// <param name="command">Update curriculum structure payload.</param>
    /// <returns>Updated curriculum structure entry.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UpdateCurriculumStructureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UpdateCurriculumStructureResponse>> UpdateCurriculumStructure(Guid id, [FromBody] UpdateCurriculumStructureCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Removes a subject from curriculum structure.
    /// </summary>
    /// <remarks>
    /// DELETE <c>api/admin/curriculum-structure/{id}</c>
    /// </remarks>
    /// <param name="id">Curriculum structure entry unique identifier.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveSubjectFromCurriculum(Guid id)
    {
        var command = new RemoveSubjectFromCurriculumCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }
}