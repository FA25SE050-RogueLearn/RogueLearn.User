﻿using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Application.Features.CurriculumPrograms.Queries.GetCurriculumProgramDetails;

namespace RogueLearn.User.Api.Controllers.Internal;

[ApiController]
[Route("api/internal/curriculum")]
[Authorize]
public class CurriculumController : ControllerBase
{
    private readonly IMediator _mediator;

    public CurriculumController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets comprehensive details of a curriculum program for internal service use.
    /// </summary>
    [HttpGet("{id:guid}/details")]
    public async Task<ActionResult<CurriculumProgramDetailsResponse>> GetCurriculumDetailsForGeneration(Guid id)
    {
        var query = new GetCurriculumProgramDetailsQuery { ProgramId = id };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}

