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
using BuildingBlocks.Shared.Authentication;

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
    public async Task<ActionResult<CreatePartyResponse>> CreateParty([FromBody] CreatePartyCommand command, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        var request = command with { CreatorAuthUserId = authUserId };
        var result = await _mediator.Send(request, cancellationToken);
        return CreatedAtAction(nameof(GetPartyById), new { partyId = result.PartyId }, result);
    }

    /// <summary>
    /// Get party by id.
    /// </summary>
    [HttpGet("{partyId:guid}")]
    [ProducesResponseType(typeof(PartyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    public async Task<ActionResult<IReadOnlyList<PartyMemberDto>>> GetMembers(Guid partyId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPartyMembersQuery(partyId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Invite a user to the party. Requires party leader.
    /// </summary>
    [HttpPost("{partyId:guid}/invite")]
    [PartyAdminOrPlatformAdmin("partyId")]
    [ProducesResponseType(typeof(PartyInvitationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PartyInvitationDto>> InviteMember(Guid partyId, [FromBody] InviteMemberRequest body, CancellationToken cancellationToken)
    {
        var inviterId = User.GetAuthUserId();
        var expiresAt = body.ExpiresAt ?? DateTimeOffset.UtcNow.AddDays(7);
        var cmd = new InviteMemberCommand(partyId, inviterId, body.InviteeAuthUserId, body.Message, expiresAt);
        var result = await _mediator.Send(cmd, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get pending invitations for a party. Requires party leader.
    /// </summary>
    [HttpGet("{partyId:guid}/invitations/pending")]
    [PartyAdminOrPlatformAdmin("partyId")]
    [ProducesResponseType(typeof(IReadOnlyList<PartyInvitationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PartyInvitationDto>>> GetPendingInvitations(Guid partyId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPendingInvitationsQuery(partyId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Add a resource to party stash. Requires party leader.
    /// </summary>
    [HttpPost("{partyId:guid}/stash")]
    [PartyAdminOrPlatformAdmin("partyId")]
    [ProducesResponseType(typeof(PartyStashItemDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PartyStashItemDto>> AddResource(Guid partyId, [FromBody] AddPartyResourceRequest body, CancellationToken cancellationToken)
    {
        var userId = User.GetAuthUserId();
        var cmd = new AddPartyResourceCommand(partyId, userId, body.Title, body.Content, body.Tags);
        var result = await _mediator.Send(cmd, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// List resources in party stash.
    /// </summary>
    [HttpGet("{partyId:guid}/stash")]
    [ProducesResponseType(typeof(IReadOnlyList<PartyStashItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PartyStashItemDto>>> GetResources(Guid partyId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPartyResourcesQuery(partyId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get all parties. Platform admin only.
    /// </summary>
    [HttpGet("/api/admin/parties")]
    [AdminOnly]
    [ProducesResponseType(typeof(IReadOnlyList<PartyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PartyDto>>> GetAllParties(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllPartiesQuery(), cancellationToken);
        return Ok(result);
    }
}