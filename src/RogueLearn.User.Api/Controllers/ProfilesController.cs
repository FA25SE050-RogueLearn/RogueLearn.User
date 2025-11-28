// RogueLearn.User/src/RogueLearn.User.Api/Controllers/ProfilesController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetAllUserProfiles;
using RogueLearn.User.Api.Attributes;
using BuildingBlocks.Shared.Authentication;
using RogueLearn.User.Application.Features.UserFullInfo.Queries.GetFullUserInfo;

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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> GetUserProfileByAuthId(Guid authId, CancellationToken cancellationToken)
    {
        var query = new GetUserProfileByAuthIdQuery(authId);
        var result = await _mediator.Send(query, cancellationToken);

        return result is not null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Gets a trimmed full profile view for a user, optimized for social features.
    /// Removes student term subjects, notes, notifications, and lecturer verification requests.
    /// </summary>
    /// <param name="authId">The user's authentication UUID.</param>
    /// <param name="pageSize">Optional page size for paged relations.</param>
    /// <param name="pageNumber">Optional page number for paged relations.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("profiles/{authId:guid}/social")]
    [ProducesResponseType(typeof(FullUserInfoSocialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FullUserInfoSocialResponse>> GetUserProfileSocial(Guid authId, [FromQuery(Name = "page[size]")] int pageSize = 20, [FromQuery(Name = "page[number]")] int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        var full = await _mediator.Send(new GetFullUserInfoQuery { AuthUserId = authId, PageSize = pageSize, PageNumber = pageNumber }, cancellationToken);
        if (full is null)
        {
            return NotFound();
        }

        var social = new FullUserInfoSocialResponse
        {
            Profile = full.Profile,
            Auth = full.Auth,
            Counts = full.Counts,
            Relations = new SocialRelationsSection
            {
                UserRoles = full.Relations.UserRoles,
                StudentEnrollments = full.Relations.StudentEnrollments,
                UserSkills = full.Relations.UserSkills,
                UserAchievements = full.Relations.UserAchievements,
                PartyMembers = full.Relations.PartyMembers,
                GuildMembers = full.Relations.GuildMembers,
                QuestAttempts = full.Relations.QuestAttempts
            }
        };

        return Ok(social);
    }

    /// <summary>
    /// Gets all user profiles (authorized users).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all user profiles.</returns>
    [HttpGet("profiles")] // Authorized non-admin access
    [ProducesResponseType(typeof(GetAllUserProfilesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetAllUserProfilesResponse>> GetAllUserProfilesAuthorized(CancellationToken cancellationToken)
    {
        var query = new GetAllUserProfilesQuery();
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }
}