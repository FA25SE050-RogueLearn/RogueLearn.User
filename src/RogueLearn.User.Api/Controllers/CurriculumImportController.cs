using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportCurriculum;
using RogueLearn.User.Application.Features.CurriculumImport.Commands.ImportSyllabus;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateCurriculum;
using RogueLearn.User.Application.Features.CurriculumImport.Queries.ValidateSyllabus;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AdminOnly]
public class CurriculumImportController : ControllerBase
{
    private readonly IMediator _mediator;

    public CurriculumImportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("curriculum")]
    public async Task<IActionResult> ImportCurriculum([FromBody] ImportCurriculumRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RawText))
        {
            return BadRequest("Raw text is required");
        }

        var command = new ImportCurriculumCommand
        {
            RawText = request.RawText,
            CreatedBy = request.CreatedBy
        };

        var result = await _mediator.Send(command);
        
        if (result.IsSuccess)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpPost("syllabus")]
    public async Task<IActionResult> ImportSyllabus([FromBody] ImportSyllabusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RawText))
        {
            return BadRequest("Raw text is required");
        }

        var command = new ImportSyllabusCommand
        {
            RawText = request.RawText,
            CreatedBy = request.CreatedBy
        };

        var result = await _mediator.Send(command);
        
        if (result.IsSuccess)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpPost("curriculum/validate")]
    public async Task<IActionResult> ValidateCurriculum([FromBody] ValidateCurriculumRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RawText))
        {
            return BadRequest("Raw text is required");
        }

        var query = new ValidateCurriculumQuery
        {
            RawText = request.RawText
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("syllabus/validate")]
    public async Task<IActionResult> ValidateSyllabus([FromBody] ValidateSyllabusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RawText))
        {
            return BadRequest("Raw text is required");
        }

        var query = new ValidateSyllabusQuery
        {
            RawText = request.RawText
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }
}

public class ImportCurriculumRequest
{
    public string RawText { get; set; } = string.Empty;
    public Guid? CreatedBy { get; set; }
}

public class ImportSyllabusRequest
{
    public string RawText { get; set; } = string.Empty;
    public Guid? CreatedBy { get; set; }
}

public class ValidateCurriculumRequest
{
    public string RawText { get; set; } = string.Empty;
}

public class ValidateSyllabusRequest
{
    public string RawText { get; set; } = string.Empty;
}