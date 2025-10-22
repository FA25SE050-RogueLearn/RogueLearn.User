// RogueLearn.User/src/RogueLearn.User.Api/Controllers/ClassSpecializationController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.ClassSpecialization.Commands.AddSpecializationSubject;
using RogueLearn.User.Application.Features.ClassSpecialization.Commands.RemoveSpecializationSubject;
using RogueLearn.User.Application.Features.ClassSpecialization.Queries.GetSpecializationSubjects;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/classes/{classId:guid}/specialization-subjects")]
[AdminOnly]
public class ClassSpecializationController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClassSpecializationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<SpecializationSubjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSpecializationSubjects(Guid classId, CancellationToken cancellationToken)
    {
        var query = new GetSpecializationSubjectsQuery { ClassId = classId };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SpecializationSubjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddSpecializationSubject(Guid classId, [FromBody] AddSpecializationSubjectCommand command, CancellationToken cancellationToken)
    {
        command.ClassId = classId;
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetSpecializationSubjects), new { classId = result.ClassId }, result);
    }

    [HttpDelete("{subjectId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveSpecializationSubject(Guid classId, Guid subjectId, CancellationToken cancellationToken)
    {
        var command = new RemoveSpecializationSubjectCommand { ClassId = classId, SubjectId = subjectId };
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}