namespace RogueLearn.User.Application.Features.RoadmapImport.Commands.ImportClassRoadmap;

public class ImportClassRoadmapResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public string? RawTextHash { get; set; }
    public string? RoadmapJson { get; set; }
    public ClassSummary? Class { get; set; }
    public int CreatedNodes { get; set; }
    public int UpdatedNodes { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ClassSummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? RoadmapUrl { get; set; }
}