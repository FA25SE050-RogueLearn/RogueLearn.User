using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetAdminQuestDetails;

public class AdminQuestDetailsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string QuestType { get; set; } = string.Empty;
    // Removed DifficultyLevel
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? SubjectCode { get; set; }
    public string? SubjectName { get; set; }

    // Grouped steps for the 3-track visualization
    public List<AdminQuestStepDto> StandardSteps { get; set; } = new();
    public List<AdminQuestStepDto> SupportiveSteps { get; set; } = new();
    public List<AdminQuestStepDto> ChallengingSteps { get; set; } = new();
}

public class AdminQuestStepDto
{
    public Guid Id { get; set; }
    public int StepNumber { get; set; }
    public int ModuleNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ExperiencePoints { get; set; }
    public string DifficultyVariant { get; set; } = string.Empty;
    public object? Content { get; set; } // Full content for admin review
    public DateTimeOffset CreatedAt { get; set; }
}