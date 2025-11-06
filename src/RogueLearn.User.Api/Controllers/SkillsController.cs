// RogueLearn.User/src/RogueLearn.User.Api/Controllers/SkillsController.cs
using BuildingBlocks.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.Skills.Queries.GetSkillTree;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/skills")] // Note: No "/admin" in the route
[Authorize] // Requires authentication, but not admin role
public class SkillsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SkillsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets the complete skill tree structure and user progress for the authenticated user.
    /// </summary>
    [HttpGet("tree")] // This now correctly combines with the controller route to become "/api/skills/tree"
    [ProducesResponseType(typeof(SkillTreeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SkillTreeDto>> GetSkillTree(CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        var query = new GetSkillTreeQuery { AuthUserId = authUserId };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}