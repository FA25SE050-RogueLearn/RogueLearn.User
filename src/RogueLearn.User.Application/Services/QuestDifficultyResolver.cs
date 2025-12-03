using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Services;

/// <summary>
/// Resolves expected quest difficulty based on user's academic performance for the related subject.
/// </summary>
public interface IQuestDifficultyResolver
{
    QuestDifficultyInfo ResolveDifficulty(StudentSemesterSubject? subjectRecord);
}

/// <summary>
/// Result of difficulty calculation for a quest.
/// </summary>
public class QuestDifficultyInfo
{
    /// <summary>
    /// Expected difficulty level: Challenging, Standard, Supportive, Adaptive
    /// </summary>
    public string ExpectedDifficulty { get; set; } = "Standard";

    /// <summary>
    /// Human-readable explanation of the difficulty assignment.
    /// </summary>
    public string DifficultyReason { get; set; } = string.Empty;

    /// <summary>
    /// The user's grade for the subject (if available).
    /// </summary>
    public string? SubjectGrade { get; set; }

    /// <summary>
    /// The user's enrollment status for the subject.
    /// </summary>
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
    /// Calculates expected quest difficulty based on the user's academic record for the subject.
    /// 
    /// Logic:
    /// - Currently Studying: Adaptive (real-time support)
    /// - Not Passed (Failed): Supportive (reinforcement focus)
    /// - Passed with grade >= 8.5: Challenging (advanced content)
    /// - Passed with grade 7.0-8.5: Standard (balanced approach)
    /// - Passed with grade < 7.0: Supportive (extra practice)
    /// - No record (first time): Standard
    /// </summary>
    public QuestDifficultyInfo ResolveDifficulty(StudentSemesterSubject? subjectRecord)
    {
        if (subjectRecord == null)
        {
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
            SubjectEnrollmentStatus.Studying => new QuestDifficultyInfo(
                expectedDifficulty: "Adaptive",
                difficultyReason: "Currently enrolled - content adapts to your progress",
                subjectGrade: gradeDisplay,
                subjectStatus: "Studying"
            ),

            SubjectEnrollmentStatus.NotPassed => new QuestDifficultyInfo(
                expectedDifficulty: "Supportive",
                difficultyReason: $"Retaking subject - focus on fundamentals and reinforcement",
                subjectGrade: gradeDisplay,
                subjectStatus: "NotPassed"
            ),

            SubjectEnrollmentStatus.Passed when grade >= 8.5 => new QuestDifficultyInfo(
                expectedDifficulty: "Challenging",
                difficultyReason: $"Excellent score ({gradeDisplay}) - advanced content with minimal scaffolding",
                subjectGrade: gradeDisplay,
                subjectStatus: "Passed"
            ),

            SubjectEnrollmentStatus.Passed when grade >= 7.0 => new QuestDifficultyInfo(
                expectedDifficulty: "Standard",
                difficultyReason: $"Good score ({gradeDisplay}) - balanced difficulty",
                subjectGrade: gradeDisplay,
                subjectStatus: "Passed"
            ),

            SubjectEnrollmentStatus.Passed => new QuestDifficultyInfo(
                expectedDifficulty: "Supportive",
                difficultyReason: $"Lower score ({gradeDisplay}) - extra practice and examples included",
                subjectGrade: gradeDisplay,
                subjectStatus: "Passed"
            ),

            SubjectEnrollmentStatus.NotStarted => new QuestDifficultyInfo(
                expectedDifficulty: "Standard",
                difficultyReason: "Not yet started - standard learning path",
                subjectGrade: null,
                subjectStatus: "NotStarted"
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
