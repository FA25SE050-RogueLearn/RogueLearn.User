using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.UserSkillRewards.Queries.GetUserSkillRewards;
using BuildingBlocks.Shared.Authentication;
using Microsoft.AspNetCore.Authorization;
using RogueLearn.User.Api.Attributes;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/users/me/skill-rewards")]
[Authorize]
public class UserSkillRewardsController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserSkillRewardsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetUserSkillRewardsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetUserSkillRewardsResponse>> Get()
    {
        // Use the authenticated user's ID from claims
        var authId = User.GetAuthUserId();

        var result = await _mediator.Send(new GetUserSkillRewardsQuery { UserId = authId });
        return Ok(result);
    }

    // Admin-only endpoint under absolute /api/admin path
    [AdminOnly]
    [HttpGet("~/api/admin/users/{id:guid}/skill-rewards")]
    [ProducesResponseType(typeof(GetUserSkillRewardsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetUserSkillRewardsResponse>> AdminGet(Guid id)
    {
        var result = await _mediator.Send(new GetUserSkillRewardsQuery { UserId = id });
        return Ok(result);
    }
}