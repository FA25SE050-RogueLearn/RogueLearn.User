// RogueLearn.User/src/RogueLearn.User.Api/Controllers/AdminCurriculumController.cs
// RogueLearn.User/src/RogueLearn.User.Api/Controllers/AdminCurriculumController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.AdminCurriculum.AnalyzeSkillDependencies;
using RogueLearn.User.Application.Features.AdminCurriculum.CreateAndMapSkills;
using RogueLearn.User.Application.Features.AdminCurriculum.SuggestSkillsFromSyllabus;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/curriculum-tools")]
[AdminOnly]
public class AdminCurriculumController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminCurriculumController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Analyzes a specific syllabus version and suggests a list of skills.
    /// This is a tool for administrators to review AI suggestions before committing.
    /// </summary>
    [HttpPost("syllabus-versions/{syllabusVersionId:guid}/suggest-skills")]
    [ProducesResponseType(typeof(SuggestSkillsFromSyllabusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SuggestSkillsFromSyllabusResponse>> SuggestSkills(Guid syllabusVersionId)
    {
        var command = new SuggestSkillsFromSyllabusCommand { SyllabusVersionId = syllabusVersionId };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Analyzes a specific syllabus version, creates any new skills found in the master catalog,
    /// and maps all relevant skills to the subject. This is an idempotent operation.
    /// </summary>
    [HttpPost("syllabus-versions/{syllabusVersionId:guid}/create-and-map-skills")]
    [ProducesResponseType(typeof(CreateAndMapSkillsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateAndMapSkillsResponse>> CreateAndMapSkills(Guid syllabusVersionId)
    {
        var command = new CreateAndMapSkillsCommand { SyllabusVersionId = syllabusVersionId };
        var result = await _mediator.Send(command);
        return Ok(result);
    }


    /// <summary>
    /// Analyzes an entire curriculum version to suggest skill dependencies.
    /// This is a tool for administrators to build the skill tree.
    /// </summary>
    [HttpPost("versions/{versionId:guid}/analyze-dependencies")]
    [ProducesResponseType(typeof(AnalyzeSkillDependenciesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnalyzeSkillDependenciesResponse>> AnalyzeDependencies(Guid versionId)
    {
        var command = new AnalyzeSkillDependenciesCommand { CurriculumVersionId = versionId };
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}