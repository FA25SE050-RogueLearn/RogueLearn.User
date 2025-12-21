using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.Onboarding.Commands.CompleteOnboarding;
using RogueLearn.User.Application.Features.Onboarding.Queries.GetAllClasses;
using RogueLearn.User.Application.Features.Onboarding.Queries.GetAllRoutes;
using BuildingBlocks.Shared.Authentication;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/onboarding")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly IMediator _mediator;

    public OnboardingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all available academic routes (curriculum programs) for user selection.
    /// </summary>
    [HttpGet("routes")]
    [ProducesResponseType(typeof(List<RouteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllRoutes(CancellationToken cancellationToken)
    {
        var query = new GetAllRoutesQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets all available career specialization classes for user selection.
    /// </summary>
    [HttpGet("classes")]
    [ProducesResponseType(typeof(List<ClassDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllClasses(CancellationToken cancellationToken)
    {
        var query = new GetAllClassesQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Completes the onboarding process for the authenticated user.
    /// </summary>
    [HttpPost("complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CompleteOnboarding([FromBody] CompleteOnboardingCommand command, CancellationToken cancellationToken)
    {
        var authUserId = User.GetAuthUserId();

        command.AuthUserId = authUserId;
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}