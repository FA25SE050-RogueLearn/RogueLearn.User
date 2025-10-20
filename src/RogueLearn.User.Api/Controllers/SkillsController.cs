using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.Skills.Commands.CreateSkill;
using RogueLearn.User.Application.Features.Skills.Commands.UpdateSkill;
using RogueLearn.User.Application.Features.Skills.Commands.DeleteSkill;
using RogueLearn.User.Application.Features.Skills.Queries.GetSkills;
using RogueLearn.User.Application.Features.Skills.Queries.GetSkillById;
using RogueLearn.User.Application.Features.SkillDependencies.Queries.GetSkillDependencies;
using RogueLearn.User.Application.Features.SkillDependencies.Commands.AddSkillDependency;
using RogueLearn.User.Application.Features.SkillDependencies.Commands.RemoveSkillDependency;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/skills")]
[AdminOnly]
public class SkillsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SkillsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<GetSkillsResponse>> GetAll()
    {
        var result = await _mediator.Send(new GetSkillsQuery());
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetSkillByIdResponse>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetSkillByIdQuery { Id = id });
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CreateSkillResponse>> Create([FromBody] CreateSkillCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UpdateSkillResponse>> Update(Guid id, [FromBody] UpdateSkillCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteSkillCommand { Id = id });
        return NoContent();
    }

    // Dependencies endpoints
    [HttpGet("{id:guid}/dependencies")]
    public async Task<ActionResult<GetSkillDependenciesResponse>> GetDependencies(Guid id)
    {
        var result = await _mediator.Send(new GetSkillDependenciesQuery { SkillId = id });
        return Ok(result);
    }

    [HttpPost("{id:guid}/dependencies")]
    public async Task<ActionResult<AddSkillDependencyResponse>> AddDependency(Guid id, [FromBody] AddSkillDependencyCommand command)
    {
        command.SkillId = id;
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetDependencies), new { id }, result);
    }

    [HttpDelete("{id:guid}/dependencies/{prereqId:guid}")]
    public async Task<IActionResult> RemoveDependency(Guid id, Guid prereqId)
    {
        await _mediator.Send(new RemoveSkillDependencyCommand { SkillId = id, PrerequisiteSkillId = prereqId });
        return NoContent();
    }
}