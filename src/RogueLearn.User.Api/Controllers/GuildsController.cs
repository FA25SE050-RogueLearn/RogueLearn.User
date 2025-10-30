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

    [HttpPost]
    [ProducesResponseType(typeof(CreateGuildResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateGuild([FromBody] CreateGuildCommand command)
    {
        var authUserId = User.GetAuthUserId();
        var req = command with { CreatorAuthUserId = authUserId };
        var result = await _mediator.Send(req);
        return CreatedAtAction(nameof(GetGuildById), new { guildId = result.GuildId }, result);
    }

    [HttpGet("{guildId:guid}")]
    [ProducesResponseType(typeof(GuildDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGuildById([FromRoute] Guid guildId)
    {
        var dto = await _mediator.Send(new GetGuildByIdQuery(guildId));
        return Ok(dto);
    }

    [HttpGet("{guildId:guid}/members")]
    [ProducesResponseType(typeof(IEnumerable<GuildMemberDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGuildMembers([FromRoute] Guid guildId)
    {
        var members = await _mediator.Send(new GetGuildMembersQuery(guildId));
        return Ok(members);
    }

    [HttpGet("{guildId:guid}/invitations", Name = "GetGuildInvitationsPublic")]
    [GuildMasterOnly("guildId")]
    [ProducesResponseType(typeof(IEnumerable<GuildInvitationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGuildInvitations([FromRoute] Guid guildId)
    {
        var invitations = await _mediator.Send(new GetGuildInvitationsQuery(guildId));
        return Ok(invitations);
    }

    // Admin-only endpoints (Game Master) with absolute routes under /api/admin/guilds
    [HttpGet("~/api/admin/guilds/{guildId:guid}/invitations", Name = "GetGuildInvitationsAdmin")]
    [AdminOnly]
    [ProducesResponseType(typeof(IEnumerable<GuildInvitationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGuildInvitationsAdmin([FromRoute] Guid guildId)
    {
        var invitations = await _mediator.Send(new GetGuildInvitationsQuery(guildId));
        return Ok(invitations);
    }

    [HttpGet("{guildId:guid}/dashboard", Name = "GetGuildDashboardPublic")]
    [GuildMasterOnly("guildId")]
    [ProducesResponseType(typeof(GuildDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGuildDashboard([FromRoute] Guid guildId)
    {
        var dashboard = await _mediator.Send(new GetGuildDashboardQuery(guildId));
        return Ok(dashboard);
    }

    [HttpGet("~/api/admin/guilds/{guildId:guid}/dashboard", Name = "GetGuildDashboardAdmin")]
    [AdminOnly]
    [ProducesResponseType(typeof(GuildDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGuildDashboardAdmin([FromRoute] Guid guildId)
    {
        var dashboard = await _mediator.Send(new GetGuildDashboardQuery(guildId));
        return Ok(dashboard);
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(GuildDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetMyGuild()
    {
        var authUserId = User.GetAuthUserId();
        var dto = await _mediator.Send(new GetMyGuildQuery(authUserId));
        if (dto == null)
        {
            return NoContent();
        }
        return Ok(dto);
    }

    [HttpPut("{guildId:guid}/settings")]
    [GuildMasterOnly("guildId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ConfigureGuildSettings([FromRoute] Guid guildId, [FromBody] ConfigureGuildSettingsCommand command)
    {
        var req = command with { GuildId = guildId };
        await _mediator.Send(req);
        return NoContent();
    }

    [HttpPut("~/api/admin/guilds/{guildId:guid}/settings")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ConfigureGuildSettingsAdmin([FromRoute] Guid guildId, [FromBody] ConfigureGuildSettingsCommand command)
    {
        var req = command with { GuildId = guildId };
        await _mediator.Send(req);
        return NoContent();
    }

    [HttpPost("{guildId:guid}/invite")]
    [GuildMasterOnly("guildId")]
    [ProducesResponseType(typeof(InviteGuildMembersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> InviteGuildMembers([FromRoute] Guid guildId, [FromBody] InviteGuildMembersCommand command)
    {
        var authUserId = User.GetAuthUserId();
        var req = command with { GuildId = guildId, InviterAuthUserId = authUserId };
        var result = await _mediator.Send(req);
        return Ok(result);
    }

    [HttpPost("~/api/admin/guilds/{guildId:guid}/invite")]
    [AdminOnly]
    [ProducesResponseType(typeof(InviteGuildMembersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> InviteGuildMembersAdmin([FromRoute] Guid guildId, [FromBody] InviteGuildMembersCommand command)
    {
        // For admin endpoints, we do not require the inviter to be a guild member
        var req = command with { GuildId = guildId, InviterAuthUserId = User.GetAuthUserId() };
        var result = await _mediator.Send(req);
        return Ok(result);
    }

    [HttpPost("{guildId:guid}/invitations/{invitationId:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AcceptInvitation([FromRoute] Guid guildId, [FromRoute] Guid invitationId)
    {
        var authUserId = User.GetAuthUserId();
        await _mediator.Send(new AcceptGuildInvitationCommand(guildId, invitationId, authUserId));
        return NoContent();
    }

    [HttpPost("{guildId:guid}/members/{memberId:guid}/remove")]
    [GuildMasterOnly("guildId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMember([FromRoute] Guid guildId, [FromRoute] Guid memberId, [FromBody] RemoveGuildMemberCommand command)
    {
        var req = command with { GuildId = guildId, MemberId = memberId };
        await _mediator.Send(req);
        return NoContent();
    }

    [HttpPost("~/api/admin/guilds/{guildId:guid}/members/{memberId:guid}/remove")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMemberAdmin([FromRoute] Guid guildId, [FromRoute] Guid memberId, [FromBody] RemoveGuildMemberCommand command)
    {
        var req = command with { GuildId = guildId, MemberId = memberId };
        await _mediator.Send(req);
        return NoContent();
    }

    [HttpPost("{guildId:guid}/transfer-leadership")]
    [GuildMasterOnly("guildId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> TransferLeadership([FromRoute] Guid guildId, [FromBody] TransferGuildLeadershipCommand command)
    {
        var req = command with { GuildId = guildId };
        await _mediator.Send(req);
        return NoContent();
    }

    [HttpPost("~/api/admin/guilds/{guildId:guid}/transfer-leadership")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> TransferLeadershipAdmin([FromRoute] Guid guildId, [FromBody] TransferGuildLeadershipCommand command)
    {
        var req = command with { GuildId = guildId };
        await _mediator.Send(req);
        return NoContent();
    }

    [HttpPost("{guildId:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LeaveGuild([FromRoute] Guid guildId)
    {
        var authUserId = User.GetAuthUserId();
        await _mediator.Send(new LeaveGuildCommand(guildId, authUserId));
        return NoContent();
    }

    [HttpDelete("{guildId:guid}")]
    [GuildMasterOnly("guildId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteGuild([FromRoute] Guid guildId)
    {
        await _mediator.Send(new DeleteGuildCommand(guildId));
        return NoContent();
    }

    [HttpDelete("~/api/admin/guilds/{guildId:guid}")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteGuildAdmin([FromRoute] Guid guildId)
    {
        await _mediator.Send(new DeleteGuildCommand(guildId));
        return NoContent();
    }

    [HttpGet("~/api/admin/guilds/{guildId:guid}")]
    [AdminOnly]
    [ProducesResponseType(typeof(GuildDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGuildByIdAdmin([FromRoute] Guid guildId)
    {
        var dto = await _mediator.Send(new GetGuildByIdQuery(guildId));
        return Ok(dto);
    }

    [HttpGet("~/api/admin/guilds/{guildId:guid}/members")]
    [AdminOnly]
    [ProducesResponseType(typeof(IEnumerable<GuildMemberDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGuildMembersAdmin([FromRoute] Guid guildId)
    {
        var members = await _mediator.Send(new GetGuildMembersQuery(guildId));
        return Ok(members);
    }
}