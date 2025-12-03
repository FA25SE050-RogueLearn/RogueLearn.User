using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.Onboarding.Queries.GetAllClasses;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/classes")]
public class ClassesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClassesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Public: Get all active classes.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ClassDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ClassDto>>> GetAll(CancellationToken cancellationToken)
    {
        var query = new GetAllClassesQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}