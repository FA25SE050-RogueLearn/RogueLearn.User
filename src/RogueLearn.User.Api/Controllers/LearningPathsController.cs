// src/RogueLearn.User/src/RogueLearn.User.Api/Controllers/LearningPathsController.cs
using BuildingBlocks.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.LearningPaths.Commands.AnalyzeLearningGap;
using RogueLearn.User.Application.Features.LearningPaths.Commands.ForgeLearningPath;
using RogueLearn.User.Application.Features.LearningPaths.Queries.GetMyLearningPath;
using RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/learning-paths")]
[Authorize]
public class LearningPathsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LearningPathsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets the primary learning path for the currently authenticated user.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(LearningPathDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LearningPathDto>> GetMyLearningPath(CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        var query = new GetMyLearningPathQuery { AuthUserId = authUserId };
        var result = await _mediator.Send(query, cancellationToken);

        return result is not null ? Ok(result) : NotFound();
    }
    /// <summary>
    /// Transaction 2: Analyzes the user's verified academic record against their
    /// chosen career class to identify the learning gap and generate a recommendation.
    /// </summary>
    [HttpPost("analyze-gap")]
    [ProducesResponseType(typeof(GapAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AnalyzeLearningGap([FromBody] Application.Models.FapRecordData recordData)
    {
        var authUserId = User.GetAuthUserId();
        var command = new AnalyzeLearningGapCommand { AuthUserId = authUserId, VerifiedRecord = recordData };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Transaction 3: Forges the high-level LearningPath structure based on the
    /// confirmed gap analysis.
    /// </summary>
    [HttpPost("forge")]
    [ProducesResponseType(typeof(ForgedLearningPath), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgeLearningPath([FromBody] ForgingPayload payload)
    {
        var authUserId = User.GetAuthUserId();
        var command = new ForgeLearningPathCommand { AuthUserId = authUserId, ForgingPayload = payload };
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetMyLearningPath), new { /* route params if any */ }, result);
    }
}