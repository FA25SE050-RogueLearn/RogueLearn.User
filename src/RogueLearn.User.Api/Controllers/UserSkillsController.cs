using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingBlocks.Shared.Authentication;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.UserSkills.Queries.GetUserSkills;
using RogueLearn.User.Application.Features.UserSkills.Queries.GetUserSkill;
using RogueLearn.User.Application.Features.UserSkills.Commands.AddUserSkill;
using RogueLearn.User.Application.Features.UserSkills.Commands.RemoveUserSkill;
using RogueLearn.User.Application.Features.UserSkills.Commands.ResetUserSkillProgress;
using RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserSkillsController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserSkillsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// List all skills for the authenticated user
    /// </summary>
    [HttpGet("me/skills")]
    [ProducesResponseType(typeof(GetUserSkillsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetUserSkillsResponse>> GetAll()
    {
        var authUserId = User.GetAuthUserId();

        var result = await _mediator.Send(new GetUserSkillsQuery
        {
            AuthUserId = authUserId
        });

        return Ok(result);
    }

    /// <summary>
    /// Get a specific skill for the authenticated user by skill name
    /// </summary>
    [HttpGet("me/skills/{skillName}")]
    [ProducesResponseType(typeof(GetUserSkillResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetUserSkillResponse>> GetOne([FromRoute] string skillName)
    {
        var authUserId = User.GetAuthUserId();

        var result = await _mediator.Send(new GetUserSkillQuery
        {
            AuthUserId = authUserId,
            SkillName = skillName
        });

        return Ok(result);
    }

    /// <summary>
    /// Track/assign a skill to the authenticated user; idempotent upsert
    /// </summary>
    [HttpPost("me/skills")]
    [ProducesResponseType(typeof(AddUserSkillResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AddUserSkillResponse>> Add([FromBody] AddUserSkillCommand command)
    {
        var authUserId = User.GetAuthUserId();

        command.AuthUserId = authUserId;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Untrack/remove a skill from the authenticated user; idempotent
    /// </summary>
    [HttpDelete("me/skills/{skillName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Remove([FromRoute] string skillName)
    {
        var authUserId = User.GetAuthUserId();

        await _mediator.Send(new RemoveUserSkillCommand
        {
            AuthUserId = authUserId,
            SkillName = skillName
        });

        return NoContent();
    }

    /// <summary>
    /// Reset progression for a user's specific skill (Admin only)
    /// </summary>
    [AdminOnly]
    [HttpPost("~/api/admin/users/{id:guid}/skills/{skillName}/reset")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AdminReset([FromRoute] Guid id, [FromRoute] string skillName, [FromBody] ResetUserSkillProgressCommand command)
    {
        // Admin-only endpoint, so we don't enforce self-access here.
        command.AuthUserId = id;
        command.SkillName = skillName;
        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Ingest an XP event for a user's skill (moved from UserAchievementsController)
    /// Only the authenticated user can ingest via this non-admin endpoint.
    /// </summary>
    [HttpPost("me/xp-events")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> IngestXpEvent([FromBody] IngestXpEventCommand command)
    {
        var authUserId = User.GetAuthUserId();

        command.AuthUserId = authUserId;
        command.SourceService = string.IsNullOrWhiteSpace(command.SourceService) ? "user-api" : command.SourceService;
        command.OccurredAt ??= DateTimeOffset.UtcNow;

        var result = await _mediator.Send(command);
        return Ok(result);
    }
}