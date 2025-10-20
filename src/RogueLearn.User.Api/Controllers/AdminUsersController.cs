using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetAllUserProfiles;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize]
[AdminOnly]
public class AdminUsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminUsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all user profiles (admin only).
    /// </summary>
    [HttpGet("profiles")]
    [ProducesResponseType(typeof(GetAllUserProfilesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetAllUserProfilesResponse>> GetAllUserProfiles(CancellationToken cancellationToken)
    {
        var query = new GetAllUserProfilesQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a user's profile by their authentication ID (admin only).
    /// </summary>
    [HttpGet("{authId:guid}")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> GetByAuthId(Guid authId, CancellationToken cancellationToken)
    {
        var query = new GetUserProfileByAuthIdQuery(authId);
        var result = await _mediator.Send(query, cancellationToken);
        return result is not null ? Ok(result) : NotFound();
    }
}