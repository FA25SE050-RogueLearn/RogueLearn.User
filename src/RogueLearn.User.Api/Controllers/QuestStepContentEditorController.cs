using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestStepContent;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/quest-steps/{questStepId:guid}")]
[AdminOnly]
public class QuestStepContentEditorController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<QuestStepContentEditorController> _logger;

    public QuestStepContentEditorController(
        IMediator mediator,
        ILogger<QuestStepContentEditorController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets the content (activities) of a quest step.
    /// Used by the admin UI to display and edit quest step content.
    /// </summary>
    [HttpGet("content")]
    [ProducesResponseType(typeof(QuestStepContentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestStepContent(
        Guid questStepId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetQuestStepContentQuery { QuestStepId = questStepId };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quest step content");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}
