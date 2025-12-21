using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Services;

public interface IAcademicContextBuilder
{
    Task<AcademicContext> BuildContextAsync(
        Guid authUserId,
        Guid targetSubjectId,
        CancellationToken cancellationToken);
}

public class AcademicContextBuilder : IAcademicContextBuilder
{
    private readonly IStudentSemesterSubjectRepository _semesterSubjectRepo;
    private readonly ISubjectRepository _subjectRepo;
    private readonly ILogger<AcademicContextBuilder> _logger;

    public AcademicContextBuilder(
        IStudentSemesterSubjectRepository semesterSubjectRepo,
        ISubjectRepository subjectRepo,
        ILogger<AcademicContextBuilder> logger)
    {
        _semesterSubjectRepo = semesterSubjectRepo;
        _subjectRepo = subjectRepo;
        _logger = logger;
    }

    public async Task<AcademicContext> BuildContextAsync(
        Guid authUserId,
        Guid targetSubjectId,
        CancellationToken cancellationToken)
    {
        var context = new AcademicContext();

        var targetSubject = await _subjectRepo.GetByIdAsync(targetSubjectId, cancellationToken);
        if (targetSubject == null)
        {
            _logger.LogWarning("Target subject {SubjectId} not found", targetSubjectId);
            return context;
        }

        var allStudentSubjects = (await _semesterSubjectRepo.FindAsync(
            ss => ss.AuthUserId == authUserId,
            cancellationToken)).ToList();

        if (!allStudentSubjects.Any())
        {
            _logger.LogInformation("No academic history for user {UserId}", authUserId);
            context.AttemptReason = QuestAttemptReason.FirstTime;
            return context;
        }

        context.CurrentGpa = CalculateGpa(allStudentSubjects);

        var targetSubjectHistory = allStudentSubjects
            .Where(ss => ss.SubjectId == targetSubjectId)
            .OrderByDescending(ss => ss.AcademicYear)
            .ToList();

        context.PreviousAttempts = targetSubjectHistory.Count;
        context.AttemptReason = DetermineAttemptReason(targetSubjectHistory);

        var allSubjects = (await _subjectRepo.GetAllAsync(cancellationToken)).ToList();
        var subjectDict = allSubjects.ToDictionary(s => s.Id, s => s);

        context.PrerequisiteHistory = AnalyzePrerequisites(
            allStudentSubjects,
            subjectDict,
            targetSubject.Semester ?? 1);

        context.RelatedSubjects = GetRelatedSubjects(
            allStudentSubjects,
            subjectDict,
            targetSubject);

        var (strengths, improvements) = AnalyzePerformancePatterns(
            allStudentSubjects,
            subjectDict);
        context.StrengthAreas = strengths;
        context.ImprovementAreas = improvements;

        return context;
    }

    private double CalculateGpa(List<StudentSemesterSubject> subjects)
    {
        var gradedSubjects = subjects
            .Where(s => s.Status == SubjectEnrollmentStatus.Passed &&
                       !string.IsNullOrEmpty(s.Grade) &&
                       double.TryParse(s.Grade, out _))
            .ToList();

        if (!gradedSubjects.Any()) return 0.0;

        var totalPoints = gradedSubjects.Sum(s => double.Parse(s.Grade!) * s.CreditsEarned);
        var totalCredits = gradedSubjects.Sum(s => s.CreditsEarned);

        return totalCredits > 0 ? totalPoints / totalCredits : 0.0;
    }

    private QuestAttemptReason DetermineAttemptReason(List<StudentSemesterSubject> history)
    {
        if (!history.Any()) return QuestAttemptReason.FirstTime;

        var latest = history.First();
        return latest.Status switch
        {
            SubjectEnrollmentStatus.NotPassed => QuestAttemptReason.Retake,
            SubjectEnrollmentStatus.Studying => QuestAttemptReason.CurrentlyStudying,
            SubjectEnrollmentStatus.Passed => QuestAttemptReason.Advancement,
            _ => QuestAttemptReason.FirstTime
        };
    }

    private List<PrerequisitePerformance> AnalyzePrerequisites(
        List<StudentSemesterSubject> allSubjects,
        Dictionary<Guid, Subject> subjectDict,
        int targetSemester)
    {
        var prerequisites = new List<PrerequisitePerformance>();

        var priorSubjects = allSubjects
            .Where(ss => subjectDict.ContainsKey(ss.SubjectId) &&
                        (subjectDict[ss.SubjectId].Semester ?? 0) < targetSemester)
            .ToList();

        foreach (var ss in priorSubjects)
        {
            if (!subjectDict.TryGetValue(ss.SubjectId, out var subject)) continue;

            var performance = new PrerequisitePerformance
            {
                SubjectCode = subject.SubjectCode,
                SubjectName = subject.SubjectName,
                Grade = ss.Grade,
                Status = ss.Status,
                PerformanceLevel = DeterminePerformanceLevel(ss)
            };

            prerequisites.Add(performance);
        }

        return prerequisites;
    }

    private List<RelatedSubjectGrade> GetRelatedSubjects(
        List<StudentSemesterSubject> allSubjects,
        Dictionary<Guid, Subject> subjectDict,
        Subject targetSubject)
    {
        var related = new List<RelatedSubjectGrade>();

        // Find subjects with similar codes (e.g., PRO192, PRO201 are related)
        var targetPrefix = ExtractSubjectPrefix(targetSubject.SubjectCode);

        foreach (var ss in allSubjects.Where(s => s.Status == SubjectEnrollmentStatus.Passed))
        {
            if (!subjectDict.TryGetValue(ss.SubjectId, out var subject)) continue;

            var subjectPrefix = ExtractSubjectPrefix(subject.SubjectCode);
            if (subjectPrefix == targetPrefix && subject.Id != targetSubject.Id)
            {
                related.Add(new RelatedSubjectGrade
                {
                    SubjectCode = subject.SubjectCode,
                    SubjectName = subject.SubjectName,
                    Grade = ss.Grade,
                    NumericGrade = TryParseGrade(ss.Grade)
                });
            }
        }

        return related;
    }

    private (List<string> Strengths, List<string> Improvements) AnalyzePerformancePatterns(
        List<StudentSemesterSubject> allSubjects,
        Dictionary<Guid, Subject> subjectDict)
    {
        var strengths = new List<string>();
        var improvements = new List<string>();

        // Group by subject prefix (e.g., PRO, MAE, CEA)
        var subjectGroups = allSubjects
            .Where(ss => subjectDict.ContainsKey(ss.SubjectId) &&
                        ss.Status == SubjectEnrollmentStatus.Passed)
            .GroupBy(ss => ExtractSubjectPrefix(subjectDict[ss.SubjectId].SubjectCode))
            .Select(g => new
            {
                Category = g.Key,
                AvgGrade = g.Average(ss => TryParseGrade(ss.Grade) ?? 0),
                Count = g.Count()
            })
            .Where(x => x.Count >= 2) // Need at least 2 subjects to establish pattern
            .ToList();

        var overallAvg = subjectGroups.Any() ? subjectGroups.Average(g => g.AvgGrade) : 0;

        foreach (var group in subjectGroups)
        {
            var categoryName = MapCategoryToName(group.Category);

            if (group.AvgGrade >= 8.0 || group.AvgGrade > overallAvg + 0.5)
            {
                strengths.Add($"{categoryName} (avg: {group.AvgGrade:F1})");
            }
            else if (group.AvgGrade < 7.0 || group.AvgGrade < overallAvg - 0.5)
            {
                improvements.Add($"{categoryName} (avg: {group.AvgGrade:F1})");
            }
        }

        return (strengths, improvements);
    }

    private string DeterminePerformanceLevel(StudentSemesterSubject ss)
    {
        if (ss.Status != SubjectEnrollmentStatus.Passed) return "Incomplete";

        var grade = TryParseGrade(ss.Grade);
        if (!grade.HasValue) return "Unknown";

        return grade.Value switch
        {
            >= 8.5 => "Strong",
            >= 7.0 => "Adequate",
            _ => "Weak"
        };
    }

    private string ExtractSubjectPrefix(string subjectCode)
    {
        // Extract letters from subject code (e.g., "PRO192" -> "PRO")
        return new string(subjectCode.TakeWhile(char.IsLetter).ToArray());
    }

    private double? TryParseGrade(string? grade)
    {
        if (string.IsNullOrWhiteSpace(grade)) return null;
        return double.TryParse(grade, out var result) ? result : null;
    }

    private string MapCategoryToName(string prefix)
    {
        return prefix switch
        {
            "PRO" => "Programming",
            "MAE" => "Mathematics",
            "CEA" => "Computer Engineering",
            "NWC" => "Networking",
            "SSG" => "System Software",
            "DBI" => "Database",
            "WED" => "Web Development",
            _ => prefix
        };
    }
}
