// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Queries/GetAllQuests/AdminQuestDto.cs
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetAllQuests;

public class AdminQuestDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string QuestType { get; set; } = string.Empty;
    public string DifficultyLevel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsRecommended { get; set; }
    public string? ExpectedDifficulty { get; set; }
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