// RogueLearn.User/src/RogueLearn.User.Api/Controllers/SubjectsController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;
using RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubject;
using RogueLearn.User.Application.Features.Subjects.Commands.DeleteSubject;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;
using RogueLearn.User.Application.Features.Subjects.Queries.GetSubjectById;
using RogueLearn.User.Application.Features.Subjects.Commands.ImportSubjectFromText;
using RogueLearn.User.Application.Features.SubjectSkillMappings.Queries.GetSubjectSkillMappings;
using RogueLearn.User.Application.Features.SubjectSkillMappings.Commands.AddSubjectSkillMapping;
using RogueLearn.User.Application.Features.SubjectSkillMappings.Commands.RemoveSubjectSkillMapping;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/subjects")]
[AdminOnly]
public class SubjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("import-from-text")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CreateSubjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateSubjectResponse>> ImportFromText([FromForm] ImportSubjectFromTextRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RawText))
        {
            return BadRequest("The 'rawText' form field is required and cannot be empty.");
        }

        var command = new ImportSubjectFromTextCommand
        {
            RawText = request.RawText,
            Semester = request.Semester
        };
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPut("import-from-text")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CreateSubjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateSubjectResponse>> UpdateFromText([FromForm] ImportSubjectFromTextRequest request, CancellationToken cancellationToken)
    {
        return await ImportFromText(request, cancellationToken);
    }

    /// <summary>
    /// Retrieves paginated subjects with optional search.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedSubjectsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedSubjectsResponse>> GetAllSubjects(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var query = new GetAllSubjectsQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search
        };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SubjectDto>> GetSubjectById(Guid id)
    {
        var query = new GetSubjectByIdQuery { Id = id };
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateSubjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateSubjectResponse>> CreateSubject([FromBody] CreateSubjectCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetSubjectById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UpdateSubjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UpdateSubjectResponse>> UpdateSubject(Guid id, [FromBody] UpdateSubjectCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteSubject(Guid id)
    {
        var command = new DeleteSubjectCommand { Id = id };
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpGet("{subjectId:guid}/skills")]
    [ProducesResponseType(typeof(List<SubjectSkillMappingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSkillMappings(Guid subjectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSubjectSkillMappingsQuery { SubjectId = subjectId }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{subjectId:guid}/skills")]
    [ProducesResponseType(typeof(AddSubjectSkillMappingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddSkillMapping(Guid subjectId, [FromBody] AddSubjectSkillMappingCommand command, CancellationToken cancellationToken)
    {
        command.SubjectId = subjectId;
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetSkillMappings), new { subjectId = result.SubjectId }, result);
    }

    [HttpDelete("{subjectId:guid}/skills/{skillId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSkillMapping(Guid subjectId, Guid skillId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveSubjectSkillMappingCommand { SubjectId = subjectId, SkillId = skillId }, cancellationToken);
        return NoContent();
    }
}