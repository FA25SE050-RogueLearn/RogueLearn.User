using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.Quests.Commands.EnsureMasterQuests;

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

    // You can move other admin-specific quest actions here later, 
    // like manually triggering content generation for a specific quest ID
    // or bulk-updating difficulty settings.
}