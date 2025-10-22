using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.CurriculumPrograms.Commands.CreateCurriculumProgram;
using RogueLearn.User.Application.Features.CurriculumPrograms.Commands.UpdateCurriculumProgram;
using RogueLearn.User.Application.Features.CurriculumPrograms.Commands.DeleteCurriculumProgram;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramById;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;
using GetAllCurriculumProgramDto = RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms.CurriculumProgramDto;
using GetByIdCurriculumProgramDto = RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetAllCurriculumPrograms.CurriculumProgramDto;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/programs")]
[AdminOnly]
/// <summary>
/// Admin endpoints for managing curriculum programs (CRUD operations).
/// </summary>
/// <remarks>
/// Access restricted via <see cref="AdminOnlyAttribute"/>.
/// </remarks>
public class CurriculumProgramsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CurriculumProgramsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    /// <summary>
    /// Retrieves all curriculum programs.
    /// </summary>
    /// <remarks>
    /// GET <c>api/admin/programs</c>
    /// </remarks>
    /// <returns>List of curriculum programs.</returns>
    [ProducesResponseType(typeof(List<GetAllCurriculumProgramDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<GetAllCurriculumProgramDto>>> GetAll()
    {
        var query = new GetAllCurriculumProgramsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    /// <summary>
    /// Retrieves a curriculum program by its identifier.
    /// </summary>
    /// <remarks>
    /// GET <c>api/admin/programs/{id}</c>
    /// </remarks>
    /// <param name="id">Program unique identifier.</param>
    /// <returns>The requested curriculum program.</returns>
    [ProducesResponseType(typeof(GetByIdCurriculumProgramDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetByIdCurriculumProgramDto>> GetById(Guid id)
    {
        var query = new GetCurriculumProgramByIdQuery { Id = id };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id}/details")]
    /// <summary>
    /// Retrieves comprehensive details of a curriculum program including all versions, subjects, and syllabus content.
    /// </summary>
    /// <remarks>
    /// GET <c>api/admin/programs/{id}/details</c>
    /// 
    /// This endpoint provides a complete overview of the curriculum program including:
    /// - All curriculum versions
    /// - All subjects in each version
    /// - All syllabus versions for each subject
    /// - Analysis of missing content at various levels
    /// </remarks>
    /// <param name="id">Program unique identifier.</param>
    /// <returns>Comprehensive curriculum program details with content analysis.</returns>
    [ProducesResponseType(typeof(CurriculumProgramDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurriculumProgramDetailsResponse>> GetDetails(Guid id)
    {
        var query = new GetCurriculumProgramDetailsQuery { ProgramId = id };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    /// <summary>
    /// Creates a new curriculum program.
    /// </summary>
    /// <remarks>
    /// POST <c>api/admin/programs</c>
    /// </remarks>
    /// <param name="command">Program creation payload.</param>
    /// <returns>Created program and its location.</returns>
    [ProducesResponseType(typeof(CreateCurriculumProgramResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateCurriculumProgramResponse>> Create([FromBody] CreateCurriculumProgramCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    /// <summary>
    /// Updates an existing curriculum program.
    /// </summary>
    /// <remarks>
    /// PUT <c>api/admin/programs/{id}</c>
    /// </remarks>
    /// <param name="id">Program unique identifier.</param>
    /// <param name="command">Program update payload.</param>
    /// <returns>Updated program details.</returns>
    [ProducesResponseType(typeof(UpdateCurriculumProgramResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UpdateCurriculumProgramResponse>> Update(Guid id, [FromBody] UpdateCurriculumProgramCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    /// <summary>
    /// Deletes a curriculum program.
    /// </summary>
    /// <remarks>
    /// DELETE <c>api/admin/programs/{id}</c>
    /// </remarks>
    /// <param name="id">Program unique identifier.</param>
    /// <returns>No content on success.</returns>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteCurriculumProgramCommand { Id = id };
        await _mediator.Send(command);
        return NoContent();
    }
}