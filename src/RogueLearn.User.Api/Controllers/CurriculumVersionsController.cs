using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.CurriculumVersions.Commands.CreateCurriculumVersion;
using RogueLearn.User.Application.Features.CurriculumVersions.Commands.ActivateCurriculumVersion;
using RogueLearn.User.Application.Features.CurriculumVersions.Queries.GetCurriculumVersionsByProgram;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/programs/{programId:guid}/versions")]
[AdminOnly]
/// <summary>
/// Admin endpoints for managing curriculum versions within a program.
/// </summary>
/// <remarks>
/// Access restricted via <see cref="AdminOnlyAttribute"/>.
/// </remarks>
public class CurriculumVersionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CurriculumVersionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    /// <summary>
    /// Lists versions for a given curriculum program.
    /// </summary>
    /// <remarks>
    /// GET <c>api/admin/programs/{programId}/versions</c>
    /// </remarks>
    /// <param name="programId">Program unique identifier.</param>
    /// <returns>List of curriculum versions.</returns>
    public async Task<ActionResult<List<CurriculumVersionDto>>> GetByProgram(Guid programId)
    {
        var query = new GetCurriculumVersionsByProgramQuery { ProgramId = programId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    /// <summary>
    /// Creates a new curriculum version for a program.
    /// </summary>
    /// <remarks>
    /// POST <c>api/admin/programs/{programId}/versions</c>
    /// </remarks>
    /// <param name="programId">Program unique identifier.</param>
    /// <param name="command">Version creation payload.</param>
    /// <returns>Created version details.</returns>
    public async Task<ActionResult<CreateCurriculumVersionResponse>> Create(Guid programId, [FromBody] CreateCurriculumVersionCommand command)
    {
        command.ProgramId = programId;
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetByProgram), new { programId = programId }, result);
    }

    [HttpPost("{versionId:guid}/activate")]
    /// <summary>
    /// Activates a curriculum version for the specified program.
    /// </summary>
    /// <remarks>
    /// POST <c>api/admin/programs/{programId}/versions/{versionId}/activate</c>
    /// </remarks>
    /// <param name="programId">Program unique identifier.</param>
    /// <param name="versionId">Version unique identifier.</param>
    /// <param name="command">Activation payload.</param>
    /// <returns>Status message confirming activation.</returns>
    public async Task<IActionResult> Activate(Guid programId, Guid versionId, [FromBody] ActivateCurriculumVersionCommand command)
    {
        command.CurriculumVersionId = versionId;
        await _mediator.Send(command);
        return Ok(new { message = "Version activated" });
    }
}