// RogueLearn.User/src/RogueLearn.User.Api/Controllers/ProfilesController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetAllUserProfiles;
using RogueLearn.User.Api.Attributes;
using BuildingBlocks.Shared.Authentication;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/")]
[Authorize]
public class ProfilesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProfilesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets the profile for the currently authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current user's profile.</returns>
    [HttpGet("profiles/me")] // ADDED THIS NEW ENDPOINT
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> GetMyProfile(CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();

        var query = new GetUserProfileByAuthIdQuery(authUserId);
        var result = await _mediator.Send(query, cancellationToken);

        return result is not null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Gets all user profiles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all user profiles.</returns>
    [HttpGet("admin/profiles")]
    [ProducesResponseType(typeof(GetAllUserProfilesResponse), StatusCodes.Status200OK)]
    [AdminOnly]
    public async Task<ActionResult<GetAllUserProfilesResponse>> GetAllUserProfiles(CancellationToken cancellationToken)
    {
        var query = new GetAllUserProfilesQuery();
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets a user profile by their authentication ID (from Supabase).
    /// </summary>
    /// <param name="authId">The user's authentication UUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user profile.</returns>
    [HttpGet("profiles/{authId:guid}")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> GetUserProfileByAuthId(Guid authId, CancellationToken cancellationToken)
    {
        var query = new GetUserProfileByAuthIdQuery(authId);
        var result = await _mediator.Send(query, cancellationToken);

        return result is not null ? Ok(result) : NotFound();
    }
}