using MediatR;
using Microsoft.AspNetCore.Mvc;
using RogueLearn.User.Api.Attributes;
using RogueLearn.User.Application.Features.RoadmapImport.Commands.ImportClassRoadmap;

namespace RogueLearn.User.Api.Controllers;

/// <summary>
/// Controller responsible for handling roadmap import operations for classes.
/// Provides endpoint for importing class roadmaps from a PDF file only.
/// </summary>
[ApiController]
[Route("api/admin")]
[AdminOnly]
public class RoadmapImportController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoadmapImportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Imports a class roadmap from a PDF file (multipart/form-data).
    /// </summary>
    /// <param name="form">Form data containing the PDF and optional metadata.</param>
    /// <returns>Import summary including class details and created/updated nodes.</returns>
    /// <response code="200">Roadmap imported successfully</response>
    /// <response code="400">Invalid request data or AI extraction failed</response>
    [HttpPost("roadmap/class")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ImportClassRoadmap([FromForm] ClassRoadmapUploadRequest form, CancellationToken cancellationToken)
    {
        if (form.Pdf is null || form.Pdf.Length == 0)
        {
            return BadRequest("A non-empty PDF file must be provided.");
        }
        if (!string.Equals(form.Pdf.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only PDF files are accepted.");
        }

        var command = new ImportClassRoadmapCommand
        {
            RoadmapUrl = form.RoadmapUrl,
            OverwriteClassMetadata = form.OverwriteClassMetadata,
            PdfAttachmentStream = form.Pdf.OpenReadStream(),
            PdfAttachmentFileName = form.Pdf.FileName,
            PdfAttachmentContentType = form.Pdf.ContentType
        };

        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result);
        }
        return BadRequest(result);
    }

    public class ClassRoadmapUploadRequest
    {
        public string? RoadmapUrl { get; set; }
        public bool OverwriteClassMetadata { get; set; } = false;
        public IFormFile? Pdf { get; set; }
    }
}