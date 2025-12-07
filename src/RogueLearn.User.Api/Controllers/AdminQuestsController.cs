// RogueLearn.User/src/RogueLearn.User.Api/Controllers/AdminQuestsController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.Quests.Commands.EnsureMasterQuests;
using RogueLearn.User.Application.Features.Quests.Queries.GetAllQuests; // ADDED

namespace RogueLearn.User.Api.Controllers;

/// <summary>
/// Admin-only endpoints for managing Master Quests and content generation.
/// </summary>
[ApiController]
[Route("api/admin/quests")]
[AdminOnly] // Applies to all methods in this controller
public class AdminQuestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminQuestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves a paginated list of all quests for admin management.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedQuestsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedQuestsResponse>> GetAllQuests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        var query = new GetAllQuestsQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            Status = status
        };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Scans all Subjects and creates a Master Quest shell for any that are missing.
    /// Use this to populate the "Quest Pool" before users generate their lines.
    /// </summary>
    [HttpPost("sync-from-subjects")]
    [ProducesResponseType(typeof(EnsureMasterQuestsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SyncMasterQuests()
    {
        var result = await _mediator.Send(new EnsureMasterQuestsCommand());
        return Ok(result);
    }
}