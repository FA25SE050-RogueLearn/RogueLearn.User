using BuildingBlocks.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;
using RogueLearn.User.Application.Features.Student.Queries.GetAcademicStatus;
using RogueLearn.User.Application.Features.UserContext.Queries.GetUserContextByAuthId;
using RogueLearn.User.Application.Features.UserProfiles.Commands.UpdateMyProfile;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetAllUserProfiles;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Features.UserFullInfo.Queries.GetFullUserInfo;

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
    /// Processes the authenticated user's raw academic record HTML to update their academic progress.
    /// This is the primary endpoint for both initial onboarding and subsequent progress syncs.
    /// </summary>
    /// <param name="fapHtmlContent">The HTML content from FAP grade report page.</param>
    /// <param name="curriculumProgramId">The curriculum program this record belongs to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Processing result with learning path and quest information.</returns>
    /// <response code="200">Academic record processed successfully.</response>
    /// <response code="400">Invalid request data or data extraction failed.</response>
    /// <response code="401">Unauthorized.</response>
    [HttpPost("me/academic-record")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProcessAcademicRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProcessAcademicRecordResponse>> ProcessMyAcademicRecord(
        [FromForm] string fapHtmlContent,
        [FromForm] Guid curriculumProgramId,
        CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();

        // The command will be updated to accept ProgramId and resolve the active version internally.
        var command = new ProcessAcademicRecordCommand
        {
            AuthUserId = authUserId,
            FapHtmlContent = fapHtmlContent,
            CurriculumProgramId = curriculumProgramId
        };

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves the authenticated user's current academic status including enrollment, subjects, and quests.
    /// This is a read-only query endpoint that does not modify any data.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete academic status information.</returns>
    /// <response code="200">Academic status retrieved successfully.</response>
    /// <response code="404">No enrollment found for the user.</response>
    /// <response code="401">Unauthorized.</response>
    [HttpGet("me/academic-status")]
    [ProducesResponseType(typeof(GetAcademicStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetAcademicStatusResponse>> GetMyAcademicStatus(CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();

        var query = new GetAcademicStatusQuery
        {
            AuthUserId = authUserId
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound(new { Message = "No enrollment found for the user." });
        }

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
    /// Retrieves the authenticated user's full dashboard info including relations and counts.
    /// </summary>
    [HttpGet("me/full")]
    [ProducesResponseType(typeof(FullUserInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FullUserInfoResponse>> GetMyFullInfo([FromQuery(Name = "page[size]")] int pageSize = 20, [FromQuery(Name = "page[number]")] int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        var authUserId = User.GetAuthUserId();
        var query = new GetFullUserInfoQuery { AuthUserId = authUserId, PageSize = pageSize, PageNumber = pageNumber };
        var result = await _mediator.Send(query, cancellationToken);
        return result is not null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Update the authenticated user's profile (multipart/form-data). Allows uploading a profile image file.
    /// The handler will process and upload the image to Supabase 'user-avatars' storage and update the profile's image URL.
    /// </summary>
    [HttpPut("me")]
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

    /// <summary>
    /// Get a user's full dashboard info by their auth ID (admin only).
    /// </summary>
    [HttpGet("~/api/admin/users/{authId:guid}/full")]
    [Authorize]
    [AdminOnly]
    [ProducesResponseType(typeof(FullUserInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FullUserInfoResponse>> AdminGetFullInfo(Guid authId, [FromQuery(Name = "page[size]")] int pageSize = 20, [FromQuery(Name = "page[number]")] int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        var query = new GetFullUserInfoQuery { AuthUserId = authId, PageSize = pageSize, PageNumber = pageNumber };
        var result = await _mediator.Send(query, cancellationToken);
        return result is not null ? Ok(result) : NotFound();
    }
}