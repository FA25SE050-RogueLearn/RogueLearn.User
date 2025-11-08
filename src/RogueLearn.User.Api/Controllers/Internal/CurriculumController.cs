// RogueLearn.User/src/RogueLearn.User.Api/Controllers/Internal/CurriculumController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.DTOs.Internal; // ADD THIS
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;

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

}