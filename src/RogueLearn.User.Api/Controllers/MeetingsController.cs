using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.Meetings.Commands.CreateOrUpdateSummary;
using RogueLearn.User.Application.Features.Meetings.Commands.ProcessArtifactsAndSummarize;
using RogueLearn.User.Application.Features.Meetings.Commands.UpsertMeeting;
using RogueLearn.User.Application.Features.Meetings.Commands.UpsertParticipants;
using RogueLearn.User.Application.Features.Meetings.Queries.GetMeetingDetails;
using RogueLearn.User.Application.Features.Meetings.DTOs;
using RogueLearn.User.Application.Features.Meetings.Queries.GetPartyMeetings;
using RogueLearn.User.Application.Features.Meetings.Queries.GetGuildMeetings;
using BuildingBlocks.Shared.Authentication;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/meetings")]
[Authorize]
public class MeetingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MeetingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<MeetingDto>> UpsertMeeting([FromBody] MeetingDto meetingDto, CancellationToken cancellationToken)
    {
        if (meetingDto == null) return BadRequest("Invalid body");
        if (meetingDto.OrganizerId == Guid.Empty)
        {
            try { meetingDto.OrganizerId = User.GetAuthUserId(); }
            catch (Exception ex) { return Unauthorized($"Unable to determine authenticated user id: {ex.Message}"); }
        }
        var result = await _mediator.Send(new UpsertMeetingCommand(meetingDto), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{meetingId}/participants")]
    public async Task<ActionResult<IReadOnlyList<MeetingParticipantDto>>> UpsertParticipants([FromRoute] Guid meetingId, [FromBody] List<MeetingParticipantDto> participants, CancellationToken cancellationToken)
    {
        if (participants == null) return BadRequest("Invalid body");
        var result = await _mediator.Send(new UpsertParticipantsCommand(meetingId, participants), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{meetingId}/artifacts")]
    public async Task<ActionResult> ProcessArtifactsAndSummarize([FromRoute] Guid meetingId, [FromBody] List<ArtifactInputDto> artifacts, CancellationToken cancellationToken)
    {
        if (artifacts == null) return BadRequest("Invalid body");
        await _mediator.Send(new ProcessArtifactsAndSummarizeCommand(meetingId, artifacts), cancellationToken);
        return Ok();
    }

    [HttpPost("{meetingId}/summary")]
    public async Task<ActionResult> CreateOrUpdateSummary([FromRoute] Guid meetingId, [FromBody] CreateSummaryRequest request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Content)) return BadRequest("Content is required");
        await _mediator.Send(new CreateOrUpdateSummaryCommand(meetingId, request.Content), cancellationToken);
        return Ok();
    }

    [HttpGet("{meetingId}")]
    public async Task<ActionResult<MeetingDetailsDto>> GetMeetingDetails([FromRoute] Guid meetingId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMeetingDetailsQuery(meetingId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("party/{partyId}")]
    public async Task<ActionResult<IReadOnlyList<MeetingDto>>> GetPartyMeetings([FromRoute] Guid partyId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPartyMeetingsQuery(partyId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("guild/{guildId}")]
    public async Task<ActionResult<IReadOnlyList<MeetingDto>>> GetGuildMeetings([FromRoute] Guid guildId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetGuildMeetingsQuery(guildId), cancellationToken);
        return Ok(result);
    }
}

public class CreateSummaryRequest
{
    public string Content { get; set; } = string.Empty;
}