using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.UserProfiles.Commands.UpdateMyProfile;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;
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
    private readonly IUserContextService _userContextService;

    public UsersController(IMediator mediator, IUserContextService userContextService)
    {
        _mediator = mediator;
        _userContextService = userContextService;
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

        var context = await _userContextService.BuildForAuthUserAsync(authUserId, cancellationToken);
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
}