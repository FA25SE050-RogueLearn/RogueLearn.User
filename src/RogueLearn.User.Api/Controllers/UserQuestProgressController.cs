using BuildingBlocks.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.QuestProgress.Queries.GetStepProgress;
using RogueLearn.User.Application.Features.QuestProgress.Queries.GetCompletedActivities;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestProgress;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/user-progress")]
[Authorize]
public class UserQuestProgressController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UserQuestProgressController> _logger;

    public UserQuestProgressController(IMediator mediator, ILogger<UserQuestProgressController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets the authenticated user's overall progress for a specific quest.
    /// Returns the list of steps filtered by the user's assigned difficulty track, with their status and lock state.
    /// </summary>
    /// <param name="questId">The quest ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of step progress details for the user's track</returns>
    [HttpGet("quests/{questId:guid}")]
    [ProducesResponseType(typeof(List<QuestStepProgressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserQuestProgress(Guid questId, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        _logger.LogInformation("Fetching overall quest progress for User:{UserId}, Quest:{QuestId}",
            authUserId, questId);

        try
        {
            var query = new GetQuestProgressQuery
            {
                AuthUserId = authUserId,
                QuestId = questId
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Application.Exceptions.NotFoundException ex)
        {
            _logger.LogWarning("Quest attempt not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching quest progress for Quest:{QuestId}", questId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets the authenticated user's progress for a specific step (module/week) including completed activities.
    /// </summary>
    /// <param name="questId">The quest ID</param>
    /// <param name="stepId">The step ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Step progress with completed activities count and percentage</returns>
    [HttpGet("quests/{questId:guid}/steps/{stepId:guid}")]
    [ProducesResponseType(typeof(GetStepProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStepProgress(Guid questId, Guid stepId, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        _logger.LogInformation("Fetching step progress for User:{UserId}, Quest:{QuestId}, Step:{StepId}",
            authUserId, questId, stepId);

        try
        {
            var query = new GetStepProgressQuery
            {
                AuthUserId = authUserId,
                QuestId = questId,
                StepId = stepId
            };
            var result = await _mediator.Send(query, cancellationToken);
            return result is not null ? Ok(result) : NotFound(new { error = "No progress found for this step." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching step progress for Step:{StepId}", stepId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets all activities in a step with their completion status for the authenticated user.
    /// </summary>
    /// <param name="questId">The quest ID</param>
    /// <param name="stepId">The step ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of activities with completion status and details</returns>
    [HttpGet("quests/{questId:guid}/steps/{stepId:guid}/activities")]
    [ProducesResponseType(typeof(GetCompletedActivitiesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStepActivities(Guid questId, Guid stepId, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        _logger.LogInformation("Fetching activities for User:{UserId}, Quest:{QuestId}, Step:{StepId}",
            authUserId, questId, stepId);

        try
        {
            var query = new GetCompletedActivitiesQuery
            {
                AuthUserId = authUserId,
                QuestId = questId,
                StepId = stepId
            };
            var result = await _mediator.Send(query, cancellationToken);
            return result is not null ? Ok(result) : NotFound(new { error = "No activities found for this step." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching activities for Step:{StepId}", stepId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}