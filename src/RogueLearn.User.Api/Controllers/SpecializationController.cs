// RogueLearn.User/src/RogueLearn.User.Api/Controllers/SpecializationController.cs
using BuildingBlocks.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.Specialization.Commands.SetUserSpecialization;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/users/me/specialization")]
[Authorize]
public class SpecializationController : ControllerBase
{
    private readonly IMediator _mediator;

    public SpecializationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Sets or updates the authenticated user's chosen career specialization (class).
    /// </summary>
    /// <param name="command">The command containing the selected classId.</param>
    /// <returns>No content on success.</returns>
    [HttpPatch]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetUserSpecialization([FromBody] SetUserSpecializationCommand command)
    {
        var authUserId = User.GetAuthUserId();
        command.AuthUserId = authUserId;

        await _mediator.Send(command);

        return NoContent();
    }
}