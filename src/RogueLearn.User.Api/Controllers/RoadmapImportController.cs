using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.RoadmapImport.Commands.ImportClassRoadmap;

namespace RogueLearn.User.Api.Controllers;

/// <summary>
/// Controller responsible for handling roadmap import operations for classes.
/// Provides endpoints for importing class roadmaps from raw text or pre-extracted JSON.
/// </summary>
[ApiController]
[Route("api/admin")]
[AdminOnly]
public class RoadmapImportController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoadmapImportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Imports a class roadmap from raw text or normalized JSON.
    /// If <see cref="ImportClassRoadmapCommand.RawText"/> is provided, AI extraction will be used to generate normalized JSON.
    /// </summary>
    /// <param name="command">The import command containing raw text or normalized JSON and optional source URL.</param>
    /// <returns>Import summary including class details and created/updated nodes.</returns>
    /// <response code="200">Roadmap imported successfully</response>
    /// <response code="400">Invalid request data or AI extraction failed</response>
    [HttpPost("roadmap/class")]
    public async Task<IActionResult> ImportClassRoadmap([FromBody] ImportClassRoadmapCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.RawText) && string.IsNullOrWhiteSpace(command.RoadmapJson))
        {
            return BadRequest("Either RawText or RoadmapJson must be provided");
        }

        var result = await _mediator.Send(command);
        if (result.IsSuccess)
        {
            return Ok(result);
        }
        return BadRequest(result);
    }
}