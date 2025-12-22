// NEW: CurriculumProgramsAdminController.cs
// Route: api/admin/programs/{programId}
// Purpose: Admin-focused curriculum program management

using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/programs/{programId:guid}")]
[AdminOnly] 
public class CurriculumProgramsAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public CurriculumProgramsAdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets comprehensive program details including:
    /// - Program metadata
    /// - All curriculum versions
    /// - Subject mappings
    /// - Specialization mappings
    /// - Content analysis (missing syllabi, coverage %)
    /// </summary>
    [HttpGet("details")]
    [ProducesResponseType(typeof(CurriculumProgramDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurriculumProgramDetailsResponse>> GetProgramDetails(
        Guid programId,
        CancellationToken cancellationToken)
    {
        var query = new GetCurriculumProgramDetailsQuery { ProgramId = programId };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}

