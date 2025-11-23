using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Application.Features.Parties.Commands.AddPartyResource;
using RogueLearn.User.Application.Features.Parties.Commands.CreateParty;
using RogueLearn.User.Application.Features.Parties.Commands.InviteMember;
using RogueLearn.User.Application.Features.Parties.Queries.GetPartyById;
using RogueLearn.User.Application.Features.Parties.Queries.GetPartyMembers;
using RogueLearn.User.Application.Features.Parties.Queries.GetPendingInvitations;
using RogueLearn.User.Application.Features.Parties.Queries.GetPartyResources;
using RogueLearn.User.Application.Features.Parties.Queries.GetAllParties;
using RogueLearn.User.Application.Features.Parties.Commands.LeaveParty;
using RogueLearn.User.Application.Features.Parties.Commands.RemoveMember;
using RogueLearn.User.Application.Features.Parties.Commands.TransferLeadership;
using RogueLearn.User.Application.Features.Parties.Commands.AcceptInvitation;
using RogueLearn.User.Application.Features.Parties.Commands.ConfigureParty;
using RogueLearn.User.Application.Features.Parties.Queries.GetMyParties;
using BuildingBlocks.Shared.Authentication;
using RogueLearn.User.Application.Features.Parties.Commands.ManageRoles;
using RogueLearn.User.Application.Features.Parties.Queries.GetMemberRoles;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Application.Features.Parties.Commands.DeleteParty;
using RogueLearn.User.Application.Features.Parties.Commands.JoinPublicParty;
using RogueLearn.User.Application.Features.Parties.Queries.GetMyPendingInvitations;
using RogueLearn.User.Application.Features.Parties.Commands.DeclineInvitation;
using RogueLearn.User.Application.Features.Parties.Commands.UpdatePartyResource;
using RogueLearn.User.Application.Features.Parties.Commands.DeletePartyResource;
using RogueLearn.User.Application.Features.Parties.Queries.GetPartyResourceById;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/parties")]
[Authorize]
public class PartiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PartiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new party. Grants creator PartyLeader role (claim issuance TBD).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreatePartyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreatePartyResponse>> CreateParty([FromBody] CreatePartyCommand command, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        var request = command with { CreatorAuthUserId = authUserId };
        var result = await _mediator.Send(request, cancellationToken);
        return CreatedAtAction(nameof(GetPartyById), new { partyId = result.PartyId }, result);
    }

    /// <summary>
    /// Leave a party.
    /// </summary>
    [HttpPost("{partyId:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LeaveParty(Guid partyId, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        await _mediator.Send(new LeavePartyCommand(partyId, authUserId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Remove a member from a party.
    /// </summary>
    [HttpPost("{partyId:guid}/members/{memberId:guid}/remove")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePartyMember(Guid partyId, Guid memberId, [FromBody] RemovePartyMemberCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command with { PartyId = partyId, MemberId = memberId }, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Transfer leadership of a party to another member.
    /// </summary>
    [HttpPost("{partyId:guid}/transfer-leadership")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransferPartyLeadership(Guid partyId, [FromBody] TransferPartyLeadershipCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command with { PartyId = partyId }, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Accept a party invitation.
    /// </summary>
    [HttpPost("{partyId:guid}/invitations/{invitationId:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptPartyInvitation(Guid partyId, Guid invitationId, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        await _mediator.Send(new AcceptPartyInvitationCommand(partyId, invitationId, authUserId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Decline a party invitation.
    /// </summary>
    [HttpPost("{partyId:guid}/invitations/{invitationId:guid}/decline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeclinePartyInvitation(Guid partyId, Guid invitationId, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        await _mediator.Send(new DeclinePartyInvitationCommand(partyId, invitationId, authUserId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Join a public party (no invitation required). Fails if party is private or at capacity.
    /// </summary>
    [HttpPost("{partyId:guid}/join")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinPublicParty([FromRoute] Guid partyId, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        await _mediator.Send(new JoinPublicPartyCommand(partyId, authUserId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Configure party settings.
    /// </summary>
    [HttpPut("{partyId:guid}")]
    [PartyLeaderOrCoLeaderOnly("partyId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfigurePartySettings(Guid partyId, [FromBody] ConfigurePartySettingsCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command with { PartyId = partyId }, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Delete a party.
    /// </summary>
    [HttpDelete("{partyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteParty(Guid partyId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeletePartyCommand(partyId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Get party by id.
    /// </summary>
    [HttpGet("{partyId:guid}")]
    [ProducesResponseType(typeof(PartyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PartyDto>> GetPartyById(Guid partyId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPartyByIdQuery(partyId), cancellationToken);
        return result is not null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// List members of a party.
    /// </summary>
    [HttpGet("{partyId:guid}/members")]
    [ProducesResponseType(typeof(IReadOnlyList<PartyMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<PartyMemberDto>>> GetMembers(Guid partyId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPartyMembersQuery(partyId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Invite a user to the party. Requires party leader.
    /// </summary>
    [HttpPost("{partyId:guid}/invite")]
    [PartyLeaderOnly("partyId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> InviteMember(Guid partyId, [FromBody] InviteMemberRequest body, CancellationToken cancellationToken)
    {
        var inviterId = User.GetAuthUserId();
        var expiresAt = body.ExpiresAt ?? DateTimeOffset.UtcNow.AddDays(7);
        var cmd = new InviteMemberCommand(partyId, inviterId, body.Targets, body.Message, expiresAt);
        await _mediator.Send(cmd, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Admin-only: Invite a user to the party.
    /// </summary>
    [HttpPost("~/api/admin/parties/{partyId:guid}/invite")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AdminInviteMember(Guid partyId, [FromBody] InviteMemberRequest body, CancellationToken cancellationToken)
    {
        var inviterId = User.GetAuthUserId();
        var expiresAt = body.ExpiresAt ?? DateTimeOffset.UtcNow.AddDays(7);
        var cmd = new InviteMemberCommand(partyId, inviterId, body.Targets, body.Message, expiresAt);
        await _mediator.Send(cmd, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Get pending invitations for a party. Requires party leader.
    /// </summary>
    [HttpGet("{partyId:guid}/invitations/pending")]
    [ProducesResponseType(typeof(IReadOnlyList<PartyInvitationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<PartyInvitationDto>>> GetPendingInvitations(Guid partyId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPendingInvitationsQuery(partyId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Admin-only: Get pending invitations for a party.
    /// </summary>
    [HttpGet("~/api/admin/parties/{partyId:guid}/invitations/pending")]
    [AdminOnly]
    [ProducesResponseType(typeof(IReadOnlyList<PartyInvitationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PartyInvitationDto>>> AdminGetPendingInvitations(Guid partyId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPendingInvitationsQuery(partyId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get my pending party invitations (invitee = current user).
    /// </summary>
    [HttpGet("invitations/pending")]
    [ProducesResponseType(typeof(IReadOnlyList<PartyInvitationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<PartyInvitationDto>>> GetMyPendingInvitations(CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        var result = await _mediator.Send(new GetMyPendingInvitationsQuery(authUserId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Add a resource to party stash. Requires party leader.
    /// </summary>
    [HttpPost("{partyId:guid}/stash")]
    [PartyLeaderOrCoLeaderOnly("partyId")]
    [ProducesResponseType(typeof(PartyStashItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PartyStashItemDto>> AddResource(Guid partyId, [FromBody] AddPartyResourceRequest body, CancellationToken cancellationToken)
    {
        var userId = User.GetAuthUserId();
        var cmd = new AddPartyResourceCommand(partyId, userId, body.OriginalNoteId, body.Title, body.Content, body.Tags);
        var result = await _mediator.Send(cmd, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Admin-only: Add a resource to party stash.
    /// </summary>
    [HttpPost("~/api/admin/parties/{partyId:guid}/stash")]
    [AdminOnly]
    [ProducesResponseType(typeof(PartyStashItemDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PartyStashItemDto>> AdminAddResource(Guid partyId, [FromBody] AddPartyResourceRequest body, CancellationToken cancellationToken)
    {
        var userId = User.GetAuthUserId();
        var cmd = new AddPartyResourceCommand(partyId, userId, body.OriginalNoteId, body.Title, body.Content, body.Tags);
        var result = await _mediator.Send(cmd, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// List resources in party stash.
    /// </summary>
    [HttpGet("{partyId:guid}/stash")]
    [ProducesResponseType(typeof(IReadOnlyList<PartyStashItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<PartyStashItemDto>>> GetResources(Guid partyId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPartyResourcesQuery(partyId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a stash item by id within a party.
    /// </summary>
    [HttpGet("{partyId:guid}/stash/{stashItemId:guid}")]
    [ProducesResponseType(typeof(PartyStashItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PartyStashItemDto>> GetResourceById([FromRoute] Guid partyId, [FromRoute] Guid stashItemId, CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(new GetPartyResourceByIdQuery(partyId, stashItemId), cancellationToken);
        return dto is not null ? Ok(dto) : NotFound();
    }

    /// <summary>
    /// Update a stash item in a party (Party Leader only).
    /// </summary>
    [HttpPut("{partyId:guid}/stash/{stashItemId:guid}")]
    [PartyLeaderOrCoLeaderOnly("partyId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateResource([FromRoute] Guid partyId, [FromRoute] Guid stashItemId, [FromBody] UpdatePartyResourceRequest body, CancellationToken cancellationToken)
    {
        var userId = User.GetAuthUserId();
        await _mediator.Send(new UpdatePartyResourceCommand(partyId, stashItemId, userId, body), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Delete a stash item from a party (Party Leader only).
    /// </summary>
    [HttpDelete("{partyId:guid}/stash/{stashItemId:guid}")]
    [PartyLeaderOrCoLeaderOnly("partyId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteResource([FromRoute] Guid partyId, [FromRoute] Guid stashItemId, CancellationToken cancellationToken)
    {
        var userId = User.GetAuthUserId();
        await _mediator.Send(new DeletePartyResourceCommand(partyId, stashItemId, userId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Admin-only: Update a party stash item.
    /// </summary>
    [HttpPut("~/api/admin/parties/{partyId:guid}/stash/{stashItemId:guid}")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AdminUpdateResource([FromRoute] Guid partyId, [FromRoute] Guid stashItemId, [FromBody] UpdatePartyResourceRequest body, CancellationToken cancellationToken)
    {
        var userId = User.GetAuthUserId();
        await _mediator.Send(new UpdatePartyResourceCommand(partyId, stashItemId, userId, body), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Admin-only: Delete a party stash item.
    /// </summary>
    [HttpDelete("~/api/admin/parties/{partyId:guid}/stash/{stashItemId:guid}")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AdminDeleteResource([FromRoute] Guid partyId, [FromRoute] Guid stashItemId, CancellationToken cancellationToken)
    {
        var userId = User.GetAuthUserId();
        await _mediator.Send(new DeletePartyResourceCommand(partyId, stashItemId, userId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Get all parties for the current user (created by user or where user is an active member).
    /// </summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<PartyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<PartyDto>>> GetMyParties(CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        var result = await _mediator.Send(new GetMyPartiesQuery(authUserId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get all parties.
    /// </summary>
    [HttpGet("/api/parties")]
    [ProducesResponseType(typeof(IReadOnlyList<PartyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<PartyDto>>> GetAllParties(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllPartiesQuery(), cancellationToken);
        return Ok(result);
    }

    // --- Role Management Endpoints ---

    /// <summary>
    /// Assign a party role to a member (Party Leader only).
    /// </summary>
    [HttpPost("{partyId:guid}/members/{memberAuthUserId:guid}/roles/assign")]
    [PartyLeaderOnly("partyId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignPartyRole([FromRoute] Guid partyId, [FromRoute] Guid memberAuthUserId, [FromBody] AssignPartyMemberRoleRequest body, CancellationToken cancellationToken)
    {
        var actorAuthUserId = User.GetAuthUserId();
        await _mediator.Send(new AssignPartyRoleCommand(partyId, memberAuthUserId, body.Role, actorAuthUserId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Revoke a party role from a member (Party Leader only). Baseline role remains.
    /// </summary>
    [HttpPost("{partyId:guid}/members/{memberAuthUserId:guid}/roles/revoke")]
    [PartyLeaderOnly("partyId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokePartyRole([FromRoute] Guid partyId, [FromRoute] Guid memberAuthUserId, [FromBody] RevokePartyMemberRoleRequest body, CancellationToken cancellationToken)
    {
        var actorAuthUserId = User.GetAuthUserId();
        await _mediator.Send(new RevokePartyRoleCommand(partyId, memberAuthUserId, body.Role, actorAuthUserId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Admin-only: Assign a party role to a member.
    /// </summary>
    [HttpPost("~/api/admin/parties/{partyId:guid}/members/{memberAuthUserId:guid}/roles/assign")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AdminAssignPartyRole([FromRoute] Guid partyId, [FromRoute] Guid memberAuthUserId, [FromBody] AssignPartyMemberRoleRequest body, CancellationToken cancellationToken)
    {
        var actorAuthUserId = User.GetAuthUserId();
        await _mediator.Send(new AssignPartyRoleCommand(partyId, memberAuthUserId, body.Role, actorAuthUserId, IsAdminOverride: true), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Admin-only: Revoke a party role from a member.
    /// </summary>
    [HttpPost("~/api/admin/parties/{partyId:guid}/members/{memberAuthUserId:guid}/roles/revoke")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AdminRevokePartyRole([FromRoute] Guid partyId, [FromRoute] Guid memberAuthUserId, [FromBody] RevokePartyMemberRoleRequest body, CancellationToken cancellationToken)
    {
        var actorAuthUserId = User.GetAuthUserId();
        await _mediator.Send(new RevokePartyRoleCommand(partyId, memberAuthUserId, body.Role, actorAuthUserId, IsAdminOverride: true), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Get roles of a party member. Returns list to support future multi-role.
    /// </summary>
    [HttpGet("{partyId:guid}/members/{memberAuthUserId:guid}/roles")]
    [ProducesResponseType(typeof(IReadOnlyList<PartyRole>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPartyMemberRoles([FromRoute] Guid partyId, [FromRoute] Guid memberAuthUserId, CancellationToken cancellationToken)
    {
        var roles = await _mediator.Send(new GetPartyMemberRolesQuery(partyId, memberAuthUserId), cancellationToken);
        return Ok(roles);
    }
}

public record AssignPartyMemberRoleRequest(PartyRole Role);
public record RevokePartyMemberRoleRequest(PartyRole Role);