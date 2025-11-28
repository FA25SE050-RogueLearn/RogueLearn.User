using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.UserRoles.Commands.AssignRoleToUser;
using RogueLearn.User.Application.Features.UserRoles.Commands.RemoveRoleFromUser;
using RogueLearn.User.Application.Features.UserRoles.Queries.GetUserRoles;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[AdminOnly]
public class UserRoleController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserRoleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Assigns a role to a user (Admin only)
    /// </summary>
    /// <param name="command">The assign role command</param>
    /// <returns>Success response</returns>
    [HttpPost("assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleToUserCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Removes a role from a user (Admin only)
    /// </summary>
    /// <param name="command">The remove role command</param>
    /// <returns>Success response</returns>
    [HttpPost("remove")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveRoleFromUser([FromBody] RemoveRoleFromUserCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Gets all roles for a specific user by auth user id (Admin only)
    /// </summary>
    /// <param name="authUserId">The auth user ID</param>
    /// <returns>User roles response</returns>
    [HttpGet("{authUserId:guid}/roles")]
    [ProducesResponseType(typeof(GetUserRolesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserRoles(Guid authUserId)
    {
        var query = new GetUserRolesQuery { AuthUserId = authUserId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}