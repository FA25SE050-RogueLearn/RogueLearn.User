using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/users")] 
[Authorize]
public class UserContextController : ControllerBase
{
    private readonly IUserContextService _userContextService;

    public UserContextController(IUserContextService userContextService)
    {
        _userContextService = userContextService;
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
    public async Task<ActionResult<UserContextDto>> GetUserContext(Guid authId, CancellationToken cancellationToken)
    {
        var context = await _userContextService.BuildForAuthUserAsync(authId, cancellationToken);
        return Ok(context);
    }
}