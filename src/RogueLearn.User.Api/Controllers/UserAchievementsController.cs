using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingBlocks.Shared.Authentication;
using RogueLearn.User.Application.Features.Achievements.Queries.GetUserAchievementsByAuthId;

using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.Achievements.Commands.AwardAchievementToUser;
using RogueLearn.User.Application.Features.Achievements.Commands.RevokeAchievementFromUser;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserAchievementsController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserAchievementsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get achievements earned by the current authenticated user
    /// </summary>
    [HttpGet("achievements/me")]
    [ProducesResponseType(typeof(GetUserAchievementsByAuthIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetUserAchievementsByAuthIdResponse>> GetMyAchievements()
    {
        var authUserId = User.GetAuthUserId();

        var result = await _mediator.Send(new GetUserAchievementsByAuthIdQuery
        {
            AuthUserId = authUserId
        });

        return Ok(result);
    }


    /// <summary>
    /// Awards an achievement to a user (Admin only)
    /// </summary>
    [AdminOnly]
    [HttpPost("~/api/admin/user-achievements/award")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Award([FromBody] AwardAchievementToUserCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Revokes an achievement from a user (Admin only)
    /// </summary>
    [AdminOnly]
    [HttpPost("~/api/admin/user-achievements/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Revoke([FromBody] RevokeAchievementFromUserCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }
}