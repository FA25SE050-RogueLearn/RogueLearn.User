// RogueLearn.User/src/RogueLearn.User.Api/Controllers/UserQuestProgressController.cs
using BuildingBlocks.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
// MODIFICATION: The namespace is now changed to the new, non-conflicting name.
using RogueLearn.User.Application.Features.QuestProgress.Queries.GetUserProgressForQuest;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/user-progress")]
[Authorize]
public class UserQuestProgressController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserQuestProgressController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets the authenticated user's progress for a specific quest, including the status of each step.
    /// </summary>
    [HttpGet("quests/{questId:guid}")]
    // MODIFICATION: The response type is updated to the new DTO name.
    [ProducesResponseType(typeof(GetUserProgressForQuestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserQuestProgress(Guid questId, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        // MODIFICATION: The query object is updated to the new name.
        var query = new GetUserProgressForQuestQuery
        {
            AuthUserId = authUserId,
            QuestId = questId
        };
        var result = await _mediator.Send(query, cancellationToken);
        return result is not null ? Ok(result) : NotFound("No progress found for this quest.");
    }
}