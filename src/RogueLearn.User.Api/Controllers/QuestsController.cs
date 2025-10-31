// RogueLearn.User/src/RogueLearn.User.Api/Controllers/QuestsController.cs
using BuildingBlocks.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestById;
using RogueLearn.User.Domain.Entities;

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
    // MODIFIED: The response type is now the new DTO.
    [ProducesResponseType(typeof(List<GeneratedQuestStepDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateQuestSteps(Guid questId)
    {
        var authUserId = User.GetAuthUserId();
        var command = new GenerateQuestStepsCommand { AuthUserId = authUserId, QuestId = questId };
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetQuestById), new { id = questId }, result);
    }
}