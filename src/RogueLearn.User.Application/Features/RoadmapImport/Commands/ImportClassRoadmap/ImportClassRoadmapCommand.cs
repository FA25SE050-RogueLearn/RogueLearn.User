using MediatR;

namespace RogueLearn.User.Application.Features.RoadmapImport.Commands.ImportClassRoadmap;

public class ImportClassRoadmapCommand : IRequest<ImportClassRoadmapResult>
{
    /// <summary>
    /// Raw text content extracted from roadmap PDF (preferred if provided).
    /// </summary>
    public string? RawText { get; set; }

    /// <summary>
    /// If provided, skip extraction and use this normalized JSON directly.
    /// </summary>
    public string? RoadmapJson { get; set; }

    /// <summary>
    /// Optional source URL to the roadmap (e.g., https://roadmap.sh/aspnet-core). Stored on Class.RoadmapUrl.
    /// </summary>
    public string? RoadmapUrl { get; set; }

    /// <summary>
    /// If true, will overwrite description and metadata on existing class. Default false.
    /// </summary>
    public bool OverwriteClassMetadata { get; set; } = false;
}