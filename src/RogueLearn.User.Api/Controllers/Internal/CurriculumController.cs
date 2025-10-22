// RogueLearn.User/src/RogueLearn.User.Api/Controllers/Internal/CurriculumController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.DTOs.Internal; // ADD THIS
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;
using RogueLearn.User.Application.Features.CurriculumVersions.Queries.GetCurriculumForQuestGeneration; // ADD THIS

namespace RogueLearn.User.Api.Controllers.Internal;

[ApiController]
[Route("api/internal/curriculum")]
[Authorize]
public class CurriculumController : ControllerBase
{
    private readonly IMediator _mediator;

    public CurriculumController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets comprehensive details of a curriculum program for admin UI use.
    /// </summary>
    [HttpGet("programs/{id:guid}/details")] // MODIFIED ROUTE
    [ProducesResponseType(typeof(CurriculumProgramDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurriculumProgramDetailsResponse>> GetCurriculumDetailsForAdmin(Guid id)
    {
        var query = new GetCurriculumProgramDetailsQuery { ProgramId = id };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets a flattened curriculum DTO for internal quest generation service.
    /// </summary>
    [HttpGet("versions/{versionId:guid}/for-quest-generation")] // NEW, EXPLICIT ENDPOINT
    [ProducesResponseType(typeof(CurriculumForQuestGenerationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurriculumForQuestGenerationDto>> GetCurriculumForQuestGeneration(Guid versionId)
    {
        var query = new GetCurriculumForQuestGenerationQuery { CurriculumVersionId = versionId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}