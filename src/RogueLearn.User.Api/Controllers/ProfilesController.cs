using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.UserProfiles.Queries.GetUserProfileByAuthId;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // This attribute protects all actions in this controller
public class ProfilesController : ControllerBase
{
	private readonly IMediator _mediator;

	public ProfilesController(IMediator mediator)
	{
		_mediator = mediator;
	}

	/// <summary>
	/// Gets a user profile by their authentication ID (from Supabase).
	/// </summary>
	/// <param name="authId">The user's authentication UUID.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The user profile.</returns>
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
