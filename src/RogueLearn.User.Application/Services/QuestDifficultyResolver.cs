// RogueLearn.User/src/RogueLearn.User.Application/Services/QuestDifficultyResolver.cs
using RogueLearn.User.Application.Models; // For AcademicAnalysisReport
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using Microsoft.Extensions.Logging; // Added for logging

namespace RogueLearn.User.Application.Services;

/// <summary>
/// Resolves expected quest difficulty based on user's academic performance AND skill proficiency.
/// </summary>
public interface IQuestDifficultyResolver
{
    // Updated signature to accept optional skill context and AI report
    // Default -1.0 indicates "No Data" / "Neutral"
    QuestDifficultyInfo ResolveDifficulty(
        StudentSemesterSubject? subjectRecord,
        double prerequisiteProficiency = -1.0,
        Subject? currentSubject = null,
        AcademicAnalysisReport? aiReport = null,
        List<string>? subjectSkills = null); // NEW: List of skill names for this subject
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
    private readonly ILogger<QuestDifficultyResolver> _logger;

    public QuestDifficultyResolver(ILogger<QuestDifficultyResolver> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculates difficulty based on grades, prerequisites, and AI analysis.
    /// </summary>
    public QuestDifficultyInfo ResolveDifficulty(
        StudentSemesterSubject? subjectRecord,
        double prerequisiteProficiency = -1.0,
        Subject? currentSubject = null,
        AcademicAnalysisReport? aiReport = null,
        List<string>? subjectSkills = null)
    {
        // Pre-calculate display values for existing record to preserve context
        string currentStatus = subjectRecord?.Status.ToString() ?? "NotStarted";
        string? currentGradeDisplay = null;
        if (subjectRecord != null)
        {
            var gradeVal = TryParseGrade(subjectRecord.Grade);
            currentGradeDisplay = gradeVal.HasValue ? $"{gradeVal.Value:F1}" : subjectRecord.Grade;
        }

        // Determine if we should allow AI to override the difficulty.
        // We allow AI adjustment if:
        // 1. The subject is new (subjectRecord == null)
        // 2. The subject is currently in progress (Studying)
        // 3. The subject is enrolled but not started (NotStarted)
        // We DO NOT override if there is a definitive outcome (Passed/NotPassed), as the actual grade should dictate the path.
        bool canApplyAiAdjustment = subjectRecord == null ||
                                    subjectRecord.Status == SubjectEnrollmentStatus.Studying ||
                                    subjectRecord.Status == SubjectEnrollmentStatus.NotStarted;

        // 1. AI Analysis Check
        if (aiReport != null && canApplyAiAdjustment && currentSubject != null)
        {
            // Build a set of keywords to check against the AI report
            // Includes: Subject Name, Subject Code, and Linked Skill Names
            var searchTerms = new List<string>();

            // Add Subject Metadata
            searchTerms.Add(currentSubject.SubjectCode);
            searchTerms.Add(currentSubject.SubjectName);

            // Add Linked Skills
            if (subjectSkills != null && subjectSkills.Any())
            {
                searchTerms.AddRange(subjectSkills);
            }

            // LOGGING START: Show what we are comparing
            _logger.LogInformation(
                "🤖 AI Difficulty Check for [{SubjectCode}]: {SubjectName}\n" +
                "   -> Status: {Status} | Can Apply AI: {CanApply}\n" +
                "   -> Search Terms: [{SearchTerms}]\n" +
                "   -> User Strong Areas: [{StrongAreas}]\n" +
                "   -> User Weak Areas: [{WeakAreas}]",
                currentSubject.SubjectCode,
                currentSubject.SubjectName,
                currentStatus,
                canApplyAiAdjustment,
                string.Join(", ", searchTerms),
                aiReport.StrongAreas != null ? string.Join(", ", aiReport.StrongAreas) : "None",
                aiReport.WeakAreas != null ? string.Join(", ", aiReport.WeakAreas) : "None");
            // LOGGING END

            // Check Weak Areas -> Supportive
            if (aiReport.WeakAreas != null)
            {
                foreach (var weakArea in aiReport.WeakAreas)
                {
                    if (MatchesAnyTerm(weakArea, searchTerms, _logger, "Weak"))
                    {
                        _logger.LogInformation("   -> MATCH FOUND (Weak): '{WeakArea}' matched a search term -> Setting SUPPORTIVE", weakArea);
                        return new QuestDifficultyInfo(
                            expectedDifficulty: "Supportive",
                            difficultyReason: $"Aligned with identified weakness: '{weakArea}'",
                            subjectGrade: currentGradeDisplay,
                            subjectStatus: currentStatus
                        );
                    }
                }
            }

            // Check Strong Areas -> Challenging
            if (aiReport.StrongAreas != null)
            {
                foreach (var strongArea in aiReport.StrongAreas)
                {
                    if (MatchesAnyTerm(strongArea, searchTerms, _logger, "Strong"))
                    {
                        _logger.LogInformation("   -> MATCH FOUND (Strong): '{StrongArea}' matched a search term -> Setting CHALLENGING", strongArea);
                        return new QuestDifficultyInfo(
                            expectedDifficulty: "Challenging",
                            difficultyReason: $"Aligned with identified strength: '{strongArea}'",
                            subjectGrade: currentGradeDisplay,
                            subjectStatus: currentStatus
                        );
                    }
                }
            }

            _logger.LogInformation("   -> No AI match found. Proceeding to standard rules.");
        }

        // 2. Check Prerequisite Proficiency (Leading Indicator)
        // If they haven't passed the subject yet, low prereq proficiency suggests they need support
        if (prerequisiteProficiency >= 0.0 && prerequisiteProficiency < 0.3 && (subjectRecord == null || subjectRecord.Status != SubjectEnrollmentStatus.Passed))
        {
            return new QuestDifficultyInfo(
                expectedDifficulty: "Supportive",
                difficultyReason: "Prerequisite skills need strengthening.",
                subjectGrade: currentGradeDisplay,
                subjectStatus: currentStatus
            );
        }

        // 3. Check Academic Record (Lagging Indicator)
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

        return subjectRecord.Status switch
        {
            SubjectEnrollmentStatus.Studying => new QuestDifficultyInfo(
                expectedDifficulty: "Standard",
                difficultyReason: "Currently enrolled - standard curriculum",
                subjectGrade: currentGradeDisplay,
                subjectStatus: "Studying"
            ),

            SubjectEnrollmentStatus.NotPassed => new QuestDifficultyInfo(
                expectedDifficulty: "Supportive",
                difficultyReason: $"Retaking subject - focus on fundamentals",
                subjectGrade: currentGradeDisplay,
                subjectStatus: "NotPassed"
            ),

            SubjectEnrollmentStatus.Passed when grade >= 8.5 => new QuestDifficultyInfo(
                expectedDifficulty: "Challenging",
                difficultyReason: $"Excellent score ({currentGradeDisplay}) - advanced content",
                subjectGrade: currentGradeDisplay,
                subjectStatus: "Passed"
            ),

            SubjectEnrollmentStatus.Passed when grade >= 7.0 => new QuestDifficultyInfo(
                expectedDifficulty: "Standard",
                difficultyReason: $"Good score ({currentGradeDisplay}) - balanced difficulty",
                subjectGrade: currentGradeDisplay,
                subjectStatus: "Passed"
            ),

            SubjectEnrollmentStatus.Passed => new QuestDifficultyInfo(
                expectedDifficulty: "Supportive",
                difficultyReason: $"Lower score ({currentGradeDisplay}) - reinforcement included",
                subjectGrade: currentGradeDisplay,
                subjectStatus: "Passed"
            ),

            _ => new QuestDifficultyInfo(
                expectedDifficulty: "Standard",
                difficultyReason: "Default difficulty",
                subjectGrade: currentGradeDisplay,
                subjectStatus: subjectRecord.Status.ToString()
            )
        };
    }

    private static double? TryParseGrade(string? grade)
    {
        if (string.IsNullOrWhiteSpace(grade)) return null;
        return double.TryParse(grade, out var result) ? result : null;
    }

    /// <summary>
    /// Checks if the 'areaText' (from AI report) contains any of the 'searchTerms' (Subject/Skills).
    /// Uses partial word matching (e.g. "Java" in "Java Web Dev" returns true).
    /// </summary>
    private static bool MatchesAnyTerm(string areaText, List<string> searchTerms, ILogger logger, string checkType)
    {
        if (string.IsNullOrWhiteSpace(areaText)) return false;
        var areaLower = areaText.ToLowerInvariant();

        foreach (var term in searchTerms)
        {
            if (string.IsNullOrWhiteSpace(term)) continue;
            var termLower = term.ToLowerInvariant();

            // Simple containment check:
            // Does "Java Web Development" (area) contain "Java" (term)? YES.
            bool match = areaLower.Contains(termLower);

            // Also check reverse for specific cases like "Java" skill matching "Java Programming"
            bool reverseMatch = termLower.Contains(areaLower);

            if (match || reverseMatch)
            {
                logger.LogDebug("      [Match Detail] Type: {Type} | Area: '{Area}' <=> Term: '{Term}' | Match: {Match} | Reverse: {Reverse}",
                    checkType, areaText, term, match, reverseMatch);
                return true;
            }
        }
        return false;
    }
}