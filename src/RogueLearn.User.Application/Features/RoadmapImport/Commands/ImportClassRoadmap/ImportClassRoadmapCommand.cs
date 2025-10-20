using MediatR;

namespace RogueLearn.User.Application.Features.RoadmapImport.Commands.ImportClassRoadmap;

public class ImportClassRoadmapCommand : IRequest<ImportClassRoadmapResponse>
{
    /// <summary>
    /// Optional source URL to the roadmap (e.g., https://roadmap.sh/aspnet-core). Stored on Class.RoadmapUrl.
    /// </summary>
    public string? RoadmapUrl { get; set; }

    /// <summary>
    /// If true, will overwrite description and metadata on existing class. Default false.
    /// </summary>
    public bool OverwriteClassMetadata { get; set; } = false;

    /// <summary>
    /// PDF file to import. This is required and will be used to extract the roadmap data.
    /// </summary>
    public Stream? PdfAttachmentStream { get; set; }
    public string? PdfAttachmentFileName { get; set; }
    public string? PdfAttachmentContentType { get; set; }
}