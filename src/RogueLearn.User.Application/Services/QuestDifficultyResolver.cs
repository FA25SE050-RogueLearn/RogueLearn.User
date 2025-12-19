// RogueLearn.User/src/RogueLearn.User.Application/Services/QuestDifficultyResolver.cs
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Services;

/// <summary>
/// Resolves expected quest difficulty based on user's academic performance AND skill proficiency.
/// </summary>
public interface IQuestDifficultyResolver
{
    // Updated signature to accept optional skill context
    QuestDifficultyInfo ResolveDifficulty(StudentSemesterSubject? subjectRecord, double prerequisiteProficiency = 1.0);
}

/// <summary>
/// Result of difficulty calculation for a quest.
/// </summary>
public class QuestDifficultyInfo
{
    public string ExpectedDifficulty { get; set; } = "Standard";
    public string DifficultyReason { get; set; } = string.Empty;
    public string? SubjectGrade { get; set; }
    public string SubjectStatus { get; set; } = "NotStarted";

    public QuestDifficultyInfo() { }

    public QuestDifficultyInfo(string expectedDifficulty, string difficultyReason, string? subjectGrade, string subjectStatus)
    {
        ExpectedDifficulty = expectedDifficulty;
        DifficultyReason = difficultyReason;
        SubjectGrade = subjectGrade;
        SubjectStatus = subjectStatus;
    }
}

public class QuestDifficultyResolver : IQuestDifficultyResolver
{
    /// <summary>
    /// Calculates difficulty.
    /// prerequisiteProficiency: 0.0 to 1.0 (Percentage of mastery of prerequisites).
    /// </summary>
    public QuestDifficultyInfo ResolveDifficulty(StudentSemesterSubject? subjectRecord, double prerequisiteProficiency = 1.0)
    {
        // 1. If Prerequisites are weak (< 30% mastery), force Supportive regardless of history.
        // This handles the "Open Access" requirement - let them in, but give them help.
        if (prerequisiteProficiency < 0.3)
        {
            return new QuestDifficultyInfo(
                expectedDifficulty: "Supportive",
                difficultyReason: "Prerequisite skills need strengthening. We've enabled extra scaffolding.",
                subjectGrade: subjectRecord?.Grade,
                subjectStatus: subjectRecord?.Status.ToString() ?? "NotStarted"
            );
        }

        if (subjectRecord == null)
        {
            // First time attempt with good prereqs -> Standard
            return new QuestDifficultyInfo(
                expectedDifficulty: "Standard",
                difficultyReason: "First attempt - standard learning path",
                subjectGrade: null,
                subjectStatus: "NotStarted"
            );
        }

        var grade = TryParseGrade(subjectRecord.Grade);
        var gradeDisplay = grade.HasValue ? $"{grade.Value:F1}" : subjectRecord.Grade ?? "N/A";

        return subjectRecord.Status switch
        {
            // Logic change: Studying usually implies they need Standard unless struggling
            SubjectEnrollmentStatus.Studying => new QuestDifficultyInfo(
                expectedDifficulty: "Standard",
                difficultyReason: "Currently enrolled - following standard curriculum",
                subjectGrade: gradeDisplay,
                subjectStatus: "Studying"
            ),

            // Failed previously -> Supportive
            SubjectEnrollmentStatus.NotPassed => new QuestDifficultyInfo(
                expectedDifficulty: "Supportive",
                difficultyReason: $"Retaking subject - focus on fundamentals and reinforcement",
                subjectGrade: gradeDisplay,
                subjectStatus: "NotPassed"
            ),

            // High Performers -> Challenging
            SubjectEnrollmentStatus.Passed when grade >= 8.5 => new QuestDifficultyInfo(
                expectedDifficulty: "Challenging",
                difficultyReason: $"Excellent score ({gradeDisplay}) - advanced content unlocked",
                subjectGrade: gradeDisplay,
                subjectStatus: "Passed"
            ),

            // Average Performers -> Standard
            SubjectEnrollmentStatus.Passed when grade >= 7.0 => new QuestDifficultyInfo(
                expectedDifficulty: "Standard",
                difficultyReason: $"Good score ({gradeDisplay}) - balanced difficulty",
                subjectGrade: gradeDisplay,
                subjectStatus: "Passed"
            ),

            // Low Pass -> Supportive (Reinforce weak pass)
            SubjectEnrollmentStatus.Passed => new QuestDifficultyInfo(
                expectedDifficulty: "Supportive",
                difficultyReason: $"Lower score ({gradeDisplay}) - extra practice included",
                subjectGrade: gradeDisplay,
                subjectStatus: "Passed"
            ),

            _ => new QuestDifficultyInfo(
                expectedDifficulty: "Standard",
                difficultyReason: "Default difficulty",
                subjectGrade: gradeDisplay,
                subjectStatus: subjectRecord.Status.ToString()
            )
        };
    }

    private static double? TryParseGrade(string? grade)
    {
        if (string.IsNullOrWhiteSpace(grade)) return null;
        return double.TryParse(grade, out var result) ? result : null;
    }
}