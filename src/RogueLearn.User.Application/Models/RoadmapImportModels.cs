namespace RogueLearn.User.Application.Models;

public class ClassRoadmapData
{
    public ClassData Class { get; set; } = new();
    public List<RoadmapNodeData> Nodes { get; set; } = new();
}

public class ClassData
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RoadmapUrl { get; set; }
    public string[]? SkillFocusAreas { get; set; }
    public int DifficultyLevel { get; set; } = 1;
    public int? EstimatedDurationMonths { get; set; }
    public bool IsActive { get; set; } = true;
}

public class RoadmapNodeData
{
    public string Title { get; set; } = string.Empty;
    public string? NodeType { get; set; }
    public string? Description { get; set; }
    public int Sequence { get; set; } = 0;
    public string FullPath { get; set; } = string.Empty;
    public List<RoadmapNodeData>? Children { get; set; }
}