// RogueLearn.User/src/RogueLearn.User.Api/Controllers/QuestStepContentEditorController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestStepContent;
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

    /// <summary>
    /// Updates the content (activities) of a quest step.
    /// Replaces the entire content object.
    /// </summary>
    [HttpPut("content")]
    [ProducesResponseType(typeof(UpdateQuestStepContentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuestStepContent(
        Guid questStepId,
        [FromBody] UpdateQuestStepContentCommand command,
        CancellationToken cancellationToken)
    {
        if (questStepId != command.QuestStepId && command.QuestStepId != Guid.Empty)
        {
            return BadRequest(new { error = "QuestStepId in route does not match body." });
        }

        command.QuestStepId = questStepId;

        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (RogueLearn.User.Application.Exceptions.ValidationException ex)
        {
            return BadRequest(new { error = "Validation failed", details = ex.Errors });
        }
        catch (RogueLearn.User.Application.Exceptions.NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quest step content");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}