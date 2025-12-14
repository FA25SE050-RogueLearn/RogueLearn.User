// RogueLearn.User/src/RogueLearn.User.Api/Controllers/StudentCurriculumController.cs
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.Student.Queries.GetClassSubjects;
using RogueLearn.User.Application.Features.Student.Queries.GetProgramSubjects;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects; // Reusing SubjectDto

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/student")]
[Authorize]
public class StudentCurriculumController : ControllerBase
{
    private readonly IMediator _mediator;

    public StudentCurriculumController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets all subjects linked to a specific curriculum program (read-only for students).
    /// Useful for displaying the "Main Route" curriculum.
    /// </summary>
    [HttpGet("programs/{programId:guid}/subjects")]
    [ProducesResponseType(typeof(List<SubjectDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProgramSubjects(Guid programId, CancellationToken cancellationToken)
    {
        var query = new GetStudentProgramSubjectsQuery { ProgramId = programId };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets all specialization subjects linked to a specific class (read-only for students).
    /// Useful for displaying the "Specialization" curriculum.
    /// </summary>
    [HttpGet("classes/{classId:guid}/subjects")]
    [ProducesResponseType(typeof(List<SubjectDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClassSubjects(Guid classId, CancellationToken cancellationToken)
    {
        var query = new GetStudentClassSubjectsQuery { ClassId = classId };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}