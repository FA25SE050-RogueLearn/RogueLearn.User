namespace RogueLearn.User.Application.Models;

public class WeekContext
{
    public int WeekNumber { get; set; }
    public int TotalWeeks { get; set; }
    public List<string> TopicsToCover { get; set; } = new();
    public List<ValidResource> AvailableResources { get; set; } = new();
}

public class ValidResource
{
    public string Url { get; set; } = string.Empty;
    public string SourceContext { get; set; } = string.Empty;
}