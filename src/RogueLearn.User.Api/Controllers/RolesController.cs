using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.Roles.Commands.CreateRole;
using RogueLearn.User.Application.Features.Roles.Commands.UpdateRole;
using RogueLearn.User.Application.Features.Roles.Commands.DeleteRole;
using RogueLearn.User.Application.Features.Roles.Queries.GetAllRoles;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/roles")]
[AdminOnly]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all roles
    /// </summary>
    /// <returns>List of all roles</returns>
    [HttpGet]
    public async Task<ActionResult<GetAllRolesResponse>> GetAllRoles()
    {
        var query = new GetAllRolesQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Create a new role
    /// </summary>
    /// <param name="command">Role creation data</param>
    /// <returns>Created role information</returns>
    [HttpPost]
    public async Task<ActionResult<CreateRoleResponse>> CreateRole([FromBody] CreateRoleCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAllRoles), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update an existing role
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <param name="command">Role update data</param>
    /// <returns>Updated role information</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<UpdateRoleResponse>> UpdateRole(Guid id, [FromBody] UpdateRoleCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Delete a role
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteRole(Guid id)
    {
        var command = new DeleteRoleCommand { Id = id };
        await _mediator.Send(command);
        return NoContent();
    }
}