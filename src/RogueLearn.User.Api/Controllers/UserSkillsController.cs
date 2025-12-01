using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingBlocks.Shared.Authentication;
using RogueLearn.User.Application.Features.UserSkills.Queries.GetUserSkills;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserSkillsController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserSkillsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// List all skills for the authenticated user
    /// </summary>
    [HttpGet("me/skills")]
    [ProducesResponseType(typeof(GetUserSkillsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetUserSkillsResponse>> GetAll()
    {
        var authUserId = User.GetAuthUserId();

        var result = await _mediator.Send(new GetUserSkillsQuery
        {
            AuthUserId = authUserId
        });

        return Ok(result);
    }
}