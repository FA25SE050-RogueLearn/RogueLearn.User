using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.SkillDependencies.Commands.AddSkillDependency;
using RogueLearn.User.Application.Features.SkillDependencies.Commands.RemoveSkillDependency;
using RogueLearn.User.Application.Features.SkillDependencies.Queries.GetSkillDependencies;
using RogueLearn.User.Application.Features.Skills.Commands.CreateSkill;
using RogueLearn.User.Application.Features.Skills.Commands.DeleteSkill;
using RogueLearn.User.Application.Features.Skills.Commands.UpdateSkill;
using RogueLearn.User.Application.Features.Skills.Queries.GetSkillById;
using RogueLearn.User.Application.Features.Skills.Queries.GetSkills;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/skills")]
[AdminOnly]
public class AdminSkillsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminSkillsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetSkillsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetSkillsResponse>> GetAll()
    {
        var result = await _mediator.Send(new GetSkillsQuery());
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetSkillByIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetSkillByIdResponse>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetSkillByIdQuery { Id = id });
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateSkillResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateSkillResponse>> Create([FromBody] CreateSkillCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UpdateSkillResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UpdateSkillResponse>> Update(Guid id, [FromBody] UpdateSkillCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteSkillCommand { Id = id });
        return NoContent();
    }

    // Dependencies endpoints
    [HttpGet("{id:guid}/dependencies")]
    [ProducesResponseType(typeof(GetSkillDependenciesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetSkillDependenciesResponse>> GetDependencies(Guid id)
    {
        var result = await _mediator.Send(new GetSkillDependenciesQuery { SkillId = id });
        return Ok(result);
    }

    [HttpPost("{id:guid}/dependencies")]
    [ProducesResponseType(typeof(AddSkillDependencyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AddSkillDependencyResponse>> AddDependency(Guid id, [FromBody] AddSkillDependencyCommand command)
    {
        command.SkillId = id;
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetDependencies), new { id }, result);
    }

    [HttpDelete("{id:guid}/dependencies/{prereqId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveDependency(Guid id, Guid prereqId)
    {
        await _mediator.Send(new RemoveSkillDependencyCommand { SkillId = id, PrerequisiteSkillId = prereqId });
        return NoContent();
    }
}