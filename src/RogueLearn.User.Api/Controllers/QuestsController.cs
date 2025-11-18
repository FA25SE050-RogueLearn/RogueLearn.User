// RogueLearn.User/src/RogueLearn.User.Api/Controllers/QuestsController.cs
using BuildingBlocks.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestById;
using RogueLearn.User.Domain.Entities;
// MODIFIED: This using is updated to point to the refactored command location.
using RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestActivityProgress;
using RogueLearn.User.Domain.Enums;
using System.Text.Json.Serialization;

[ApiController]
[Route("api/quests")]
[Authorize]
public class QuestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public QuestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(QuestDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestById(Guid id)
    {
        var result = await _mediator.Send(new GetQuestByIdQuery { Id = id });
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpPost("{questId:guid}/generate-steps")]
    [ProducesResponseType(typeof(List<GeneratedQuestStepDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateQuestSteps(Guid questId)
    {
        var authUserId = User.GetAuthUserId();
        var command = new GenerateQuestStepsCommand { AuthUserId = authUserId, QuestId = questId };
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetQuestById), new { id = questId }, result);
    }

    // MODIFIED: This endpoint is now more specific. It targets a specific activity within a step.
    /// <summary>
    /// Updates the progress status of a specific activity within a quest step (weekly module) for the authenticated user.
    /// </summary>
    [HttpPost("{questId:guid}/steps/{stepId:guid}/activities/{activityId:guid}/progress")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuestActivityProgress(Guid questId, Guid stepId, Guid activityId, [FromBody] UpdateQuestActivityProgressRequest body)
    {
        var authUserId = User.GetAuthUserId();
        // MODIFIED: We now use the new, more descriptive command.
        var command = new UpdateQuestActivityProgressCommand
        {
            AuthUserId = authUserId,
            QuestId = questId,
            StepId = stepId,
            ActivityId = activityId,
            Status = body.Status
        };
        await _mediator.Send(command);
        return NoContent();
    }
}

// MODIFIED: Renamed request DTO for clarity.
public class UpdateQuestActivityProgressRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StepCompletionStatus Status { get; set; }
}