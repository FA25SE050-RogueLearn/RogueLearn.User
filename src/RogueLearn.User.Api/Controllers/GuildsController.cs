using System.Net.Mime;
using BuildingBlocks.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.Guilds.Commands.CreateGuild;
using RogueLearn.User.Application.Features.Guilds.Commands.DeleteGuild;
using RogueLearn.User.Application.Features.Guilds.Commands.LeaveGuild;
using RogueLearn.User.Application.Features.Guilds.Commands.AcceptInvitation;
using RogueLearn.User.Application.Features.Guilds.Commands.ConfigureGuild;
using RogueLearn.User.Application.Features.Guilds.Commands.InviteMember;
using RogueLearn.User.Application.Features.Guilds.Commands.RemoveMember;
using RogueLearn.User.Application.Features.Guilds.Commands.TransferLeadership;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Application.Features.Guilds.Queries.GetGuildById;
using RogueLearn.User.Application.Features.Guilds.Queries.GetGuildDashboard;
using RogueLearn.User.Application.Features.Guilds.Queries.GetGuildInvitations;
using RogueLearn.User.Application.Features.Guilds.Queries.GetGuildMembers;
using RogueLearn.User.Application.Features.Guilds.Queries.GetMyGuild;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/guilds")]
[Produces(MediaTypeNames.Application.Json)]
public class GuildsController : ControllerBase
{
    private readonly IMediator _mediator;

    public GuildsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new guild. The authenticated user becomes the guild leader.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateGuildResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateGuild([FromBody] CreateGuildCommand command, CancellationToken cancellationToken = default)
    {
        var authUserId = User.GetAuthUserId();
        var req = command with { CreatorAuthUserId = authUserId };
        var result = await _mediator.Send(req, cancellationToken);
        return CreatedAtAction(nameof(GetGuildById), new { guildId = result.GuildId }, result);
    }

    /// <summary>
    /// Get a guild by its id.
    /// </summary>
    [HttpGet("{guildId:guid}")]
    [ProducesResponseType(typeof(GuildDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetGuildById([FromRoute] Guid guildId, CancellationToken cancellationToken = default)
    {
        var dto = await _mediator.Send(new GetGuildByIdQuery(guildId), cancellationToken);
        return dto is not null ? Ok(dto) : NotFound();
    }

    /// <summary>
    /// List members of the guild.
    /// </summary>
    [HttpGet("{guildId:guid}/members")]
    [ProducesResponseType(typeof(IEnumerable<GuildMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetGuildMembers([FromRoute] Guid guildId, CancellationToken cancellationToken = default)
    {
        var members = await _mediator.Send(new GetGuildMembersQuery(guildId), cancellationToken);
        return Ok(members);
    }

    /// <summary>
    /// Get pending invitations for the guild (Guild Master only).
    /// </summary>
    [HttpGet("{guildId:guid}/invitations", Name = "GetGuildInvitationsPublic")]
    [GuildMasterOnly("guildId")]
    [ProducesResponseType(typeof(IEnumerable<GuildInvitationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetGuildInvitations([FromRoute] Guid guildId, CancellationToken cancellationToken = default)
    {
        var invitations = await _mediator.Send(new GetGuildInvitationsQuery(guildId), cancellationToken);
        return Ok(invitations);
    }

    // Admin-only endpoints (Game Master) with absolute routes under /api/admin/guilds
    /// <summary>
    /// Admin-only: Get pending invitations for a guild.
    /// </summary>
    [HttpGet("~/api/admin/guilds/{guildId:guid}/invitations", Name = "GetGuildInvitationsAdmin")]
    [AdminOnly]
    [ProducesResponseType(typeof(IEnumerable<GuildInvitationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetGuildInvitationsAdmin([FromRoute] Guid guildId, CancellationToken cancellationToken = default)
    {
        var invitations = await _mediator.Send(new GetGuildInvitationsQuery(guildId), cancellationToken);
        return Ok(invitations);
    }

    /// <summary>
    /// Get guild dashboard (Guild Master only).
    /// </summary>
    [HttpGet("{guildId:guid}/dashboard", Name = "GetGuildDashboardPublic")]
    [GuildMasterOnly("guildId")]
    [ProducesResponseType(typeof(GuildDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetGuildDashboard([FromRoute] Guid guildId, CancellationToken cancellationToken = default)
    {
        var dashboard = await _mediator.Send(new GetGuildDashboardQuery(guildId), cancellationToken);
        return Ok(dashboard);
    }

    /// <summary>
    /// Admin-only: Get guild dashboard.
    /// </summary>
    [HttpGet("~/api/admin/guilds/{guildId:guid}/dashboard", Name = "GetGuildDashboardAdmin")]
    [AdminOnly]
    [ProducesResponseType(typeof(GuildDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetGuildDashboardAdmin([FromRoute] Guid guildId, CancellationToken cancellationToken = default)
    {
        var dashboard = await _mediator.Send(new GetGuildDashboardQuery(guildId), cancellationToken);
        return Ok(dashboard);
    }

    /// <summary>
    /// Get the guild the authenticated user belongs to.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(GuildDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyGuild(CancellationToken cancellationToken = default)
    {
        var authUserId = User.GetAuthUserId();
        var dto = await _mediator.Send(new GetMyGuildQuery(authUserId), cancellationToken);
        if (dto == null)
        {
            return NoContent();
        }
        return Ok(dto);
    }

    /// <summary>
    /// Update guild settings (Guild Master only).
    /// </summary>
    [HttpPut("{guildId:guid}/settings")]
    [GuildMasterOnly("guildId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ConfigureGuildSettings([FromRoute] Guid guildId, [FromBody] ConfigureGuildSettingsCommand command, CancellationToken cancellationToken = default)
    {
        var req = command with { GuildId = guildId };
        await _mediator.Send(req, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Admin-only: Update guild settings.
    /// </summary>
    [HttpPut("~/api/admin/guilds/{guildId:guid}/settings")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ConfigureGuildSettingsAdmin([FromRoute] Guid guildId, [FromBody] ConfigureGuildSettingsCommand command, CancellationToken cancellationToken = default)
    {
        var req = command with { GuildId = guildId };
        await _mediator.Send(req, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Invite members to the guild (Guild Master only).
    /// </summary>
    [HttpPost("{guildId:guid}/invite")]
    [GuildMasterOnly("guildId")]
    [ProducesResponseType(typeof(InviteGuildMembersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> InviteGuildMembers([FromRoute] Guid guildId, [FromBody] InviteGuildMembersCommand command, CancellationToken cancellationToken = default)
    {
        var authUserId = User.GetAuthUserId();
        var req = command with { GuildId = guildId, InviterAuthUserId = authUserId };
        var result = await _mediator.Send(req, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Admin-only: Invite members to a guild.
    /// </summary>
    [HttpPost("~/api/admin/guilds/{guildId:guid}/invite")]
    [AdminOnly]
    [ProducesResponseType(typeof(InviteGuildMembersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> InviteGuildMembersAdmin([FromRoute] Guid guildId, [FromBody] InviteGuildMembersCommand command, CancellationToken cancellationToken = default)
    {
        // For admin endpoints, we do not require the inviter to be a guild member
        var req = command with { GuildId = guildId, InviterAuthUserId = User.GetAuthUserId() };
        var result = await _mediator.Send(req, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Accept a guild invitation.
    /// </summary>
    [HttpPost("{guildId:guid}/invitations/{invitationId:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AcceptInvitation([FromRoute] Guid guildId, [FromRoute] Guid invitationId, CancellationToken cancellationToken = default)
    {
        var authUserId = User.GetAuthUserId();
        await _mediator.Send(new AcceptGuildInvitationCommand(guildId, invitationId, authUserId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Remove a member from the guild (Guild Master only).
    /// </summary>
    [HttpPost("{guildId:guid}/members/{memberId:guid}/remove")]
    [GuildMasterOnly("guildId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveMember([FromRoute] Guid guildId, [FromRoute] Guid memberId, [FromBody] RemoveGuildMemberCommand command, CancellationToken cancellationToken = default)
    {
        var req = command with { GuildId = guildId, MemberId = memberId };
        await _mediator.Send(req, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Admin-only: Remove a member from a guild.
    /// </summary>
    [HttpPost("~/api/admin/guilds/{guildId:guid}/members/{memberId:guid}/remove")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveMemberAdmin([FromRoute] Guid guildId, [FromRoute] Guid memberId, [FromBody] RemoveGuildMemberCommand command, CancellationToken cancellationToken = default)
    {
        var req = command with { GuildId = guildId, MemberId = memberId };
        await _mediator.Send(req, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Transfer guild leadership to another member (Guild Master only).
    /// </summary>
    [HttpPost("{guildId:guid}/transfer-leadership")]
    [GuildMasterOnly("guildId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TransferLeadership([FromRoute] Guid guildId, [FromBody] TransferGuildLeadershipCommand command, CancellationToken cancellationToken = default)
    {
        var req = command with { GuildId = guildId };
        await _mediator.Send(req, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Admin-only: Transfer guild leadership.
    /// </summary>
    [HttpPost("~/api/admin/guilds/{guildId:guid}/transfer-leadership")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TransferLeadershipAdmin([FromRoute] Guid guildId, [FromBody] TransferGuildLeadershipCommand command, CancellationToken cancellationToken = default)
    {
        var req = command with { GuildId = guildId };
        await _mediator.Send(req, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Leave the guild the authenticated user is currently in.
    /// </summary>
    [HttpPost("{guildId:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LeaveGuild([FromRoute] Guid guildId, CancellationToken cancellationToken = default)
    {
        var authUserId = User.GetAuthUserId();
        await _mediator.Send(new LeaveGuildCommand(guildId, authUserId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Delete a guild (Guild Master only).
    /// </summary>
    [HttpDelete("{guildId:guid}")]
    [GuildMasterOnly("guildId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteGuild([FromRoute] Guid guildId, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteGuildCommand(guildId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Admin-only: Delete a guild.
    /// </summary>
    [HttpDelete("~/api/admin/guilds/{guildId:guid}")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteGuildAdmin([FromRoute] Guid guildId, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteGuildCommand(guildId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Admin-only: Get a guild by id.
    /// </summary>
    [HttpGet("~/api/admin/guilds/{guildId:guid}")]
    [AdminOnly]
    [ProducesResponseType(typeof(GuildDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetGuildByIdAdmin([FromRoute] Guid guildId, CancellationToken cancellationToken = default)
    {
        var dto = await _mediator.Send(new GetGuildByIdQuery(guildId), cancellationToken);
        return dto is not null ? Ok(dto) : NotFound();
    }

    /// <summary>
    /// Admin-only: List members of a guild.
    /// </summary>
    [HttpGet("~/api/admin/guilds/{guildId:guid}/members")]
    [AdminOnly]
    [ProducesResponseType(typeof(IEnumerable<GuildMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetGuildMembersAdmin([FromRoute] Guid guildId, CancellationToken cancellationToken = default)
    {
        var members = await _mediator.Send(new GetGuildMembersQuery(guildId), cancellationToken);
        return Ok(members);
    }
}