// RogueLearn.User/src/RogueLearn.User.Api/Controllers/AcademicRecordsController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.AcademicRecords.Commands.ExtractFapRecord;
using RogueLearn.User.Application.Models;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/academic-records")]
[Authorize]
public class AcademicRecordsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AcademicRecordsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Transaction 1: Extracts structured academic data from raw FAP HTML
    /// without persisting it. This is a read-only analysis step.
    /// </summary>
    [HttpPost("extract")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(FapRecordData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExtractFapRecord([FromForm] string fapHtmlContent)
    {
        // This command now correctly implements IRequest<FapRecordData>.
        var command = new ExtractFapRecordCommand { FapHtmlContent = fapHtmlContent };

        // The 'result' variable will now be correctly typed as FapRecordData.
        var result = await _mediator.Send(command);

        return Ok(result);
    }
}