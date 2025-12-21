using BuildingBlocks.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.Skills.Queries.GetSkillTree;
using RogueLearn.User.Application.Features.Skills.Queries.GetSkillDetail;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/skills")]
[Authorize]
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
    [HttpGet("tree")]
    [ProducesResponseType(typeof(SkillTreeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SkillTreeDto>> GetSkillTree(CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        var query = new GetSkillTreeQuery { AuthUserId = authUserId };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets detailed information for a specific skill, including prerequisites, unlocks, and linked quests.
    /// </summary>
    /// <param name="skillId">The UUID of the skill.</param>
    /// <returns>Detailed skill view.</returns>
    [HttpGet("{skillId:guid}/details")]
    [ProducesResponseType(typeof(SkillDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SkillDetailDto>> GetSkillDetail(Guid skillId, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        var query = new GetSkillDetailQuery { AuthUserId = authUserId, SkillId = skillId };
        var result = await _mediator.Send(query, cancellationToken);

        return result is not null ? Ok(result) : NotFound();
    }
}