﻿// src/RogueLearn.User/src/RogueLearn.User.Api/Controllers/LearningPathsController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingBlocks.Shared.Authentication;
using RogueLearn.User.Application.Features.LearningPaths.Queries.GetMyLearningPath;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/learning-paths")]
[Authorize]
public class LearningPathsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LearningPathsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets the primary learning path for the currently authenticated user.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(LearningPathDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LearningPathDto>> GetMyLearningPath(CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();
        var query = new GetMyLearningPathQuery { AuthUserId = authUserId };
        var result = await _mediator.Send(query, cancellationToken);

        return result is not null ? Ok(result) : NotFound();
    }
}