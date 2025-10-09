using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.UserRoles.Commands.AssignRoleToUser;
using RogueLearn.User.Application.Features.UserRoles.Commands.RemoveRoleFromUser;
using RogueLearn.User.Application.Features.UserRoles.Queries.GetUserRoles;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleToUserCommand command)
    {
        await _mediator.Send(command);
        return Ok(new { message = "Role assigned successfully" });
    }

    /// <summary>
    /// Removes a role from a user (Admin only)
    /// </summary>
    /// <param name="command">The remove role command</param>
    /// <returns>Success response</returns>
    [HttpPost("remove")]
    public async Task<IActionResult> RemoveRoleFromUser([FromBody] RemoveRoleFromUserCommand command)
    {
        await _mediator.Send(command);
        return Ok(new { message = "Role removed successfully" });
    }

    /// <summary>
    /// Gets all roles for a specific user (Admin only)
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>User roles response</returns>
    [HttpGet("user/{userId:guid}/roles")]
    public async Task<IActionResult> GetUserRoles(Guid userId)
    {
        var query = new GetUserRolesQuery { UserId = userId };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}