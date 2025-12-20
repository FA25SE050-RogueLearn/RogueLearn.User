using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.ClassSpecialization.Commands.DeleteClass;
using RogueLearn.User.Application.Features.ClassSpecialization.Commands.UpdateClass;
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

    /// <summary>
    /// Admin: Update an existing class.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [AdminOnly]
    [ProducesResponseType(typeof(ClassDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ClassDto>> Update(Guid id, [FromBody] UpdateClassCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest("ID mismatch");
        }
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Admin: Delete a class.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteClassCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}