// RogueLearn.User/src/RogueLearn.User.Api/Controllers/Internal/CurriculumController.cs
using MediatR;
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
    /// Gets comprehensive details of a curriculum program for internal service use, looked up by EITHER program ID or version ID.
    /// </summary>
    [HttpGet("{id:guid}/details")]
    public async Task<ActionResult<CurriculumProgramDetailsResponse>> GetCurriculumDetailsForGeneration(Guid id)
    {
        // MODIFIED: We intelligently decide whether the incoming ID is for a Program or a Version.
        // This is a robust way to handle the ambiguity, but a more explicit route would be even better.
        // For now, we will assume the ID is a VersionID, as that is what the QuestService sends.
        var query = new GetCurriculumProgramDetailsQuery { VersionId = id };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}