// RogueLearn.User/src/RogueLearn.User.Application/Features/LearningPaths/Queries/GetMyLearningPath/LearningPathDto.cs
namespace RogueLearn.User.Application.Features.LearningPaths.Queries.GetMyLearningPath;

public class LearningPathDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<QuestChapterDto> Chapters { get; set; } = new();
    public double CompletionPercentage { get; set; }
}

public class QuestChapterDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<QuestSummaryDto> Quests { get; set; } = new();
}

public class QuestSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    // ADDED: Include the parent LearningPath ID for context.
    public Guid LearningPathId { get; set; }
    // ADDED: Include the parent Chapter ID for constructing navigation URLs.
    public Guid ChapterId { get; set; }
    public bool IsRecommended { get; set; }
    public string? RecommendationReason { get; set; }

    /// <summary>
    /// Expected difficulty based on user's academic performance: Challenging, Standard, Supportive, Adaptive
    /// </summary>
    public string ExpectedDifficulty { get; set; } = "Standard";

    /// <summary>
    /// Human-readable explanation of why this difficulty was assigned.
    /// </summary>
    public string? DifficultyReason { get; set; }

    /// <summary>
    /// User's grade for this subject (if available).
    /// </summary>
    public string? SubjectGrade { get; set; }

    /// <summary>
    /// User's enrollment status for this subject: Passed, NotPassed, Studying, NotStarted
    /// </summary>
    public string? SubjectStatus { get; set; }
}