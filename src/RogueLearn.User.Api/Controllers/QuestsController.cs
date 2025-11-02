// RogueLearn.User/src/RogueLearn.User.Api/Controllers/QuestsController.cs
using BuildingBlocks.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestById;
using RogueLearn.User.Domain.Entities;
// ADDED: New using directives for the update commands
using RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestProgress;
using RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestStepProgress;
using RogueLearn.User.Domain.Enums;

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

    // MODIFICATION: Added a new endpoint for manually updating quest progress.
    /// <summary>
    /// Manually updates the status of a specific quest for the authenticated user.
    /// </summary>
    [HttpPost("{questId:guid}/progress")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuestProgress(Guid questId, [FromBody] UpdateQuestProgressRequest body)
    {
        var authUserId = User.GetAuthUserId();
        var command = new UpdateQuestProgressCommand
        {
            AuthUserId = authUserId,
            QuestId = questId,
            Status = body.Status
        };
        await _mediator.Send(command);
        return NoContent();
    }

    // NEW ENDPOINT: Marks a specific quest step's progress for the authenticated user.
    /// <summary>
    /// Updates the progress status of a specific quest step for the authenticated user.
    /// </summary>
    [HttpPost("{questId:guid}/steps/{stepId:guid}/progress")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuestStepProgress(Guid questId, Guid stepId, [FromBody] UpdateQuestStepProgressRequest body)
    {
        var authUserId = User.GetAuthUserId();
        var command = new UpdateQuestStepProgressCommand
        {
            AuthUserId = authUserId,
            QuestId = questId,
            StepId = stepId,
            Status = body.Status
        };
        await _mediator.Send(command);
        return NoContent();
    }
}

// DTO for the new endpoint's request body
public class UpdateQuestProgressRequest
{
    public QuestStatus Status { get; set; }
}

// NEW DTO: Request body for updating a quest step.
public class UpdateQuestStepProgressRequest
{
    public StepCompletionStatus Status { get; set; }
}