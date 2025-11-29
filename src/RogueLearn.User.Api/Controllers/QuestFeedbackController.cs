// src/RogueLearn.User.Api/Controllers/QuestFeedbackController.cs
using BuildingBlocks.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.QuestFeedback.Commands.SubmitQuestStepFeedback;
using RogueLearn.User.Application.Features.QuestFeedback.Queries.GetQuestFeedbackList;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class QuestFeedbackController : ControllerBase
{
    private readonly IMediator _mediator;

    public QuestFeedbackController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Submit feedback for a specific quest step.
    /// </summary>
    [HttpPost("quests/{questId:guid}/steps/{stepId:guid}/feedback")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitFeedback(
        Guid questId,
        Guid stepId,
        [FromBody] SubmitQuestStepFeedbackCommand command)
    {
        command.AuthUserId = User.GetAuthUserId();
        command.QuestId = questId;
        command.StepId = stepId;

        var feedbackId = await _mediator.Send(command);
        return CreatedAtAction(nameof(AdminGetFeedback), new { id = feedbackId }, new { id = feedbackId });
    }

    /// <summary>
    /// Admin-only: Get feedback list.
    /// </summary>
    [HttpGet("admin/quests/feedback")]
    [AdminOnly]
    [ProducesResponseType(typeof(List<QuestFeedbackDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<QuestFeedbackDto>>> AdminGetFeedback(
        [FromQuery] Guid? questId,
        [FromQuery] bool unresolvedOnly = true)
    {
        var query = new GetQuestFeedbackListQuery
        {
            QuestId = questId,
            UnresolvedOnly = unresolvedOnly
        };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}