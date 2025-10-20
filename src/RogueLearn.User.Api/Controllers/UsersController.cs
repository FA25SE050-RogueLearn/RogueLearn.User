using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.UserContext.Queries.GetUserContextByAuthId;
using RogueLearn.User.Application.Features.UserProfiles.Commands.UpdateMyProfile;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetAllUserProfiles;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using System.Security.Claims;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get the authenticated user's aggregated context (profile, roles, skills, enrollment, etc.).
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserContextDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserContextDto>> GetMyContext(CancellationToken cancellationToken)
    {
        var authIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(authIdClaim) || !Guid.TryParse(authIdClaim, out var authUserId))
        {
            return Unauthorized();
        }

        var context = await _mediator.Send(new GetUserContextByAuthIdQuery(authUserId), cancellationToken);
        return Ok(context);
    }

    /// <summary>
    /// Update the authenticated user's profile (multipart/form-data). Allows uploading a profile image file.
    /// The handler will process and upload the image to Supabase 'user-avatars' storage and update the profile's image URL.
    /// </summary>
    [HttpPatch("me")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> PatchMyProfileForm([FromForm] UpdateMyProfileCommand command, IFormFile? profileImage, CancellationToken cancellationToken)
    {
        var authIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(authIdClaim) || !Guid.TryParse(authIdClaim, out var authUserId))
        {
            return Unauthorized();
        }

        // If an image is provided, pass its bytes and metadata to the command for the handler to process
        if (profileImage is not null && profileImage.Length > 0)
        {
            using var ms = new MemoryStream();
            await profileImage.CopyToAsync(ms, cancellationToken);
            command.ProfileImageBytes = ms.ToArray();
            command.ProfileImageContentType = profileImage.ContentType;
            command.ProfileImageFileName = profileImage.FileName;
        }

        command.AuthUserId = authUserId;
        var updated = await _mediator.Send(command, cancellationToken);
        return Ok(updated);
    }

    // ===== Admin endpoints moved from AdminUsersController =====

    /// <summary>
    /// Get all user profiles (admin only).
    /// </summary>
    [HttpGet("~/api/admin/users/profiles")]
    [Authorize]
    [AdminOnly]
    [ProducesResponseType(typeof(GetAllUserProfilesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetAllUserProfilesResponse>> AdminGetAllUserProfiles(CancellationToken cancellationToken)
    {
        var query = new GetAllUserProfilesQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a user's profile by their authentication ID (admin only).
    /// </summary>
    [HttpGet("~/api/admin/users/{authId:guid}")]
    [Authorize]
    [AdminOnly]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> AdminGetByAuthId(Guid authId, CancellationToken cancellationToken)
    {
        var query = new GetUserProfileByAuthIdQuery(authId);
        var result = await _mediator.Send(query, cancellationToken);
        return result is not null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Get a user's aggregated context by their auth ID (admin only).
    /// </summary>
    /// <param name="authId">The user's authentication UUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{authId:guid}/context")]
    [AdminOnly]
    [ProducesResponseType(typeof(UserContextDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserContextDto>> AdminGetUserContext(Guid authId, CancellationToken cancellationToken)
    {
        var context = await _mediator.Send(new GetUserContextByAuthIdQuery(authId), cancellationToken);
        return context is not null ? Ok(context) : NotFound();
    }
}