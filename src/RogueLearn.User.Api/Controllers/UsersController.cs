// RogueLearn.User/src/RogueLearn.User.Api/Controllers/UsersController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.UserContext.Queries.GetUserContextByAuthId;
using RogueLearn.User.Application.Features.UserProfiles.Commands.UpdateMyProfile;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetAllUserProfiles;
using RogueLearn.User.Application.Models;
using BuildingBlocks.Shared.Authentication;
using RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;

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
    /// Processes the authenticated user's raw academic record HTML to update their academic progress and generate/update their questline.
    /// Accepts multipart/form-data to handle potentially large or complex HTML content.
    /// </summary>
    [HttpPost("me/process-academic-record")]
    [Consumes("multipart/form-data")] // MODIFIED: Changed from application/json to multipart/form-data
    [ProducesResponseType(typeof(ProcessAcademicRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProcessAcademicRecordResponse>> ProcessMyAcademicRecord(
        [FromForm] string fapHtmlContent, // MODIFIED: Attribute changed to [FromForm]
        [FromForm] Guid curriculumVersionId, // MODIFIED: Attribute changed to [FromForm]
        CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();

        // MODIFIED: Command is now constructed from form fields instead of a JSON body.
        var command = new ProcessAcademicRecordCommand
        {
            AuthUserId = authUserId,
            FapHtmlContent = fapHtmlContent,
            CurriculumVersionId = curriculumVersionId
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get the authenticated user's aggregated context (profile, roles, skills, enrollment, etc.).
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserContextDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserContextDto>> GetMyContext(CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();

        var context = await _mediator.Send(new GetUserContextByAuthIdQuery(authUserId), cancellationToken);
        return context is not null ? Ok(context) : NotFound();
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
        var authUserId = User.GetAuthUserId();

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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    [HttpGet("~/api/admin/users/{authId:guid}/context")]
    [Authorize]
    [AdminOnly]
    [ProducesResponseType(typeof(UserContextDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserContextDto>> AdminGetUserContext(Guid authId, CancellationToken cancellationToken)
    {
        var context = await _mediator.Send(new GetUserContextByAuthIdQuery(authId), cancellationToken);
        return context is not null ? Ok(context) : NotFound();
    }
}