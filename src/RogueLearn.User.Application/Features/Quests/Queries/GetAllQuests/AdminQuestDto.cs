using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetAllQuests;

public class AdminQuestDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string QuestType { get; set; } = string.Empty;
    // Removed DifficultyLevel
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    // Removed IsRecommended
    // Removed ExpectedDifficulty
    public string? SubjectCode { get; set; }
    public string? SubjectName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class PaginatedQuestsResponse
{
    public List<AdminQuestDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}