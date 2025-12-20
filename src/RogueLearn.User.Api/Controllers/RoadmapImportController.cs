using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.ClassSpecialization.Commands.CreateClassFromRoadmap;
using RogueLearn.User.Application.Features.Onboarding.Queries.GetAllClasses;

namespace RogueLearn.User.Api.Controllers;

[ApiController]
[Route("api/admin/roadmap-import")]
[AdminOnly]
public class RoadmapImportController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoadmapImportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new Class Specialization by analyzing a roadmap.sh PDF.
    /// </summary>
    /// <param name="file">The PDF file downloaded from roadmap.sh.</param>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ClassDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ClassDto>> ImportRoadmap(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is required.");

        if (file.ContentType != "application/pdf")
            return BadRequest("Only PDF files are supported.");

        var command = new CreateClassFromRoadmapCommand
        {
            FileStream = file.OpenReadStream(),
            FileName = file.FileName,
            ContentType = file.ContentType
        };

        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(ClassesController.GetAll), "Classes", null, result);
    }
}