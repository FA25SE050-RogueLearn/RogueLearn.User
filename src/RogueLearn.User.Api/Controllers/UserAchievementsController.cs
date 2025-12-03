using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingBlocks.Shared.Authentication;
using RogueLearn.User.Application.Features.Achievements.Queries.GetUserAchievementsByAuthId;

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
}