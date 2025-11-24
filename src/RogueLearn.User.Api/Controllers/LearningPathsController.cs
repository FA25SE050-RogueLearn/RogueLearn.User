// RogueLearn.User/src/RogueLearn.User.Api/Controllers/LearningPathsController.cs
using BuildingBlocks.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
// MODIFICATION: The following using statements are no longer needed and have been removed.
// using RogueLearn.User.Application.Features.LearningPaths.Commands.AnalyzeLearningGap;
// using RogueLearn.User.Application.Features.LearningPaths.Commands.ForgeLearningPath;
using RogueLearn.User.Application.Features.LearningPaths.Commands.DeleteLearningPath;
using RogueLearn.User.Application.Features.LearningPaths.Queries.GetMyLearningPath;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Api.Attributes;

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
    [ResponseCache(CacheProfileName = "Default60", VaryByHeader = "Authorization")]
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

    // MODIFICATION: The 'AnalyzeLearningGap' endpoint has been removed.

    // MODIFICATION: The 'ForgeLearningPath' endpoint has been removed.

    /// <summary>
    /// Admin-only: Deletes a learning path by its ID, including related quest chapters and quests.
    /// </summary>
    [HttpDelete("~/api/admin/learning-paths/{id}")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLearningPath(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteLearningPathCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}