using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.Onboarding.Queries.GetAllClasses;
using RogueLearn.User.Application.Features.Classes.Queries.GetClasses;
using RogueLearn.User.Application.Features.Classes.Queries.GetClassById;
using RogueLearn.User.Application.Features.Classes.Commands.CreateClass;
using RogueLearn.User.Application.Features.Classes.Commands.UpdateClass;
using RogueLearn.User.Application.Features.Classes.Commands.SoftDeleteClass;
using RogueLearn.User.Application.Features.Classes.Commands.RestoreClass;
using RogueLearn.User.Application.Features.Classes.DTOs;

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

    // ===== Admin endpoints retained under /api/admin/classes =====

    /// <summary>
    /// Admin: List classes with optional active filter.
    /// </summary>
    [HttpGet("~/api/admin/classes")]
    [Authorize]
    [AdminOnly]
    [ProducesResponseType(typeof(List<ClassDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ClassDetailDto>>> AdminGetAll([FromQuery] bool? active, CancellationToken cancellationToken)
    {
        var classes = await _mediator.Send(new GetClassesQuery(active), cancellationToken);
        var result = classes.Select(c => ClassDetailDto.FromEntity(c)).OrderBy(c => c.Name).ToList();
        return Ok(result);
    }

    /// <summary>
    /// Admin: Get a class by id.
    /// </summary>
    [HttpGet("~/api/admin/classes/{id:guid}")]
    [Authorize]
    [AdminOnly]
    [ProducesResponseType(typeof(ClassDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ClassDetailDto>> AdminGetById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _mediator.Send(new GetClassByIdQuery(id), cancellationToken);
        if (entity is null)
            return NotFound();
        return Ok(ClassDetailDto.FromEntity(entity));
    }

    /// <summary>
    /// Admin: Create a new class.
    /// </summary>
    [HttpPost("~/api/admin/classes")]
    [Authorize]
    [AdminOnly]
    [ProducesResponseType(typeof(ClassDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ClassDetailDto>> AdminCreate([FromBody] CreateClassCommand command, CancellationToken cancellationToken)
    {
        var entity = await _mediator.Send(command, cancellationToken);
        var dto = ClassDetailDto.FromEntity(entity);
        return CreatedAtAction(nameof(AdminGetById), new { id = entity.Id }, dto);
    }

    /// <summary>
    /// Admin: Update an existing class by id.
    /// </summary>
    [HttpPut("~/api/admin/classes/{id:guid}")]
    [Authorize]
    [AdminOnly]
    [ProducesResponseType(typeof(ClassDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ClassDetailDto>> AdminUpdate(Guid id, [FromBody] UpdateClassCommand command, CancellationToken cancellationToken)
    {
        var effectiveCommand = command with { Id = id };
        var entity = await _mediator.Send(effectiveCommand, cancellationToken);
        return Ok(ClassDetailDto.FromEntity(entity));
    }

    /// <summary>
    /// Admin: Soft delete a class (sets is_active=false).
    /// </summary>
    [HttpDelete("~/api/admin/classes/{id:guid}")]
    [Authorize]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AdminSoftDelete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SoftDeleteClassCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Admin: Restore a soft-deleted class.
    /// </summary>
    [HttpPost("~/api/admin/classes/{id:guid}/restore")]
    [Authorize]
    [AdminOnly]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AdminRestore(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RestoreClassCommand(id), cancellationToken);
        return NoContent();
    }

}