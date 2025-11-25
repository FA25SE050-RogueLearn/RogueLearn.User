using System.Net.Mime;
using BuildingBlocks.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Commands.CreateLecturerVerificationRequest;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Queries.GetMyLecturerVerificationRequests;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Queries.AdminListLecturerVerificationRequests;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Queries.AdminGetLecturerVerificationRequest;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Commands.ApproveLecturerVerificationRequest;
using RogueLearn.User.Application.Features.LecturerVerificationRequests.Commands.DeclineLecturerVerificationRequest;
using RogueLearn.User.Application.Features.LecturerVerificationRequests;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Authorize]
[Produces(MediaTypeNames.Application.Json)]
[Route("api/lecturer-verification")] 
public class LecturerVerificationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILecturerVerificationProofStorage _proofStorage;

    public LecturerVerificationController(IMediator mediator, ILecturerVerificationProofStorage proofStorage)
    {
        _mediator = mediator;
        _proofStorage = proofStorage;
    }

    [HttpPost("requests")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CreateLecturerVerificationRequestResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateRequest([FromBody] CreateLecturerVerificationRequestCommand command, CancellationToken cancellationToken)
    {
        command.AuthUserId = User.GetAuthUserId();
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("requests/form")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CreateLecturerVerificationRequestResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateRequestForm([FromForm] CreateLecturerVerificationFormData form, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        string? screenshotUrl = null;
        if (form.Screenshot is not null)
        {
            using var ms = new MemoryStream();
            await form.Screenshot.CopyToAsync(ms, cancellationToken);
            screenshotUrl = await _proofStorage.UploadAsync(authUserId, ms.ToArray(), form.Screenshot.ContentType, form.Screenshot.FileName, cancellationToken);
        }
        var cmd = new CreateLecturerVerificationRequestCommand
        {
            AuthUserId = authUserId,
            Email = form.Email,
            StaffId = form.StaffId,
            ScreenshotUrl = screenshotUrl
        };
        var result = await _mediator.Send(cmd, cancellationToken);
        return Ok(result);
    }


    [HttpGet("requests")]
    [ProducesResponseType(typeof(IReadOnlyList<MyLecturerVerificationRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyRequests(CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        var list = await _mediator.Send(new GetMyLecturerVerificationRequestsQuery { AuthUserId = authUserId }, cancellationToken);
        return Ok(list);
    }

    [HttpGet("/api/admin/lecturer-verification/requests")]
    [AdminOnly]
    [ProducesResponseType(typeof(AdminListLecturerVerificationRequestsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AdminList([FromQuery] string? status, [FromQuery] Guid? userId, [FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new AdminListLecturerVerificationRequestsQuery { Status = status, UserId = userId, Page = page, Size = size }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("/api/admin/lecturer-verification/requests/{requestId:guid}")]
    [AdminOnly]
    [ProducesResponseType(typeof(AdminLecturerVerificationRequestDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminGet(Guid requestId, CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(new AdminGetLecturerVerificationRequestQuery { RequestId = requestId }, cancellationToken);
        return dto is not null ? Ok(dto) : NotFound();
    }

    [HttpPost("/api/admin/lecturer-verification/requests/{requestId:guid}/approve")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AdminApprove(Guid requestId, [FromBody] ApproveLecturerVerificationRequestCommand body, CancellationToken cancellationToken)
    {
        var reviewerId = User.GetAuthUserId();
        await _mediator.Send(new ApproveLecturerVerificationRequestCommand { RequestId = requestId, ReviewerAuthUserId = reviewerId, Note = body.Note }, cancellationToken);
        return NoContent();
    }

    [HttpPost("/api/admin/lecturer-verification/requests/{requestId:guid}/decline")]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AdminDecline(Guid requestId, [FromBody] DeclineLecturerVerificationRequestCommand body, CancellationToken cancellationToken)
    {
        var reviewerId = User.GetAuthUserId();
        await _mediator.Send(new DeclineLecturerVerificationRequestCommand { RequestId = requestId, ReviewerAuthUserId = reviewerId, Reason = body.Reason }, cancellationToken);
        return NoContent();
    }
}