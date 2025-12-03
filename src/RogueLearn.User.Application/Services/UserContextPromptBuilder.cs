using System.Text;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Services;

public interface IPromptBuilder
{
    Task<string> GenerateAsync(UserProfile userProfile, Class userClass, AcademicContext academicContext, CancellationToken cancellationToken = default);
}

public class UserContextPromptBuilder : IPromptBuilder
{
    private readonly IStudentSemesterSubjectRepository _semesterSubjectRepository;
    private readonly ISubjectRepository _subjectRepository;

    public UserContextPromptBuilder(
        IStudentSemesterSubjectRepository semesterSubjectRepository,
        ISubjectRepository subjectRepository)
    {
        _semesterSubjectRepository = semesterSubjectRepository;
        _subjectRepository = subjectRepository;
    }

    public async Task<string> GenerateAsync(UserProfile userProfile, Class userClass, AcademicContext academicContext, CancellationToken cancellationToken = default)
    {
        var semesterSubjects = (await _semesterSubjectRepository.FindAsync(
            ss => ss.AuthUserId == userProfile.AuthUserId, cancellationToken)).ToList();

        if (!semesterSubjects.Any())
        {
            return "No academic records found for this student.";
        }

        var subjectIds = semesterSubjects.Select(ss => ss.SubjectId).Distinct().ToList();
        var allSubjects = (await _subjectRepository.GetAllAsync(cancellationToken))
            .Where(s => subjectIds.Contains(s.Id))
            .ToDictionary(s => s.Id);

        var prompt = new StringBuilder();
        prompt.AppendLine("## Student Performance Summary");
        prompt.AppendLine();

        // Add user information
        prompt.AppendLine($"**Student:** {userProfile.FirstName} {userProfile.LastName} ({userProfile.Username})");
        prompt.AppendLine($"**Level:** {userProfile.Level}");
        prompt.AppendLine($"**Experience Points:** {userProfile.ExperiencePoints}");
        prompt.AppendLine();

        // Add class/roadmap information
        prompt.AppendLine("### Learning Track & Career Roadmap");
        prompt.AppendLine($"**Learning Track:** {userClass.Name}");
        if (!string.IsNullOrEmpty(userClass.Description))
        {
            prompt.AppendLine($"**Track Description:** {userClass.Description}");
        }
        if (!string.IsNullOrEmpty(userClass.RoadmapUrl))
        {
            prompt.AppendLine($"**Industry Roadmap Reference:** {userClass.RoadmapUrl}");
            prompt.AppendLine("_(This roadmap from roadmap.sh defines industry-standard skills and learning progression for this career path)_");
        }
        prompt.AppendLine($"**Track Difficulty:** {userClass.DifficultyLevel}");
        if (userClass.EstimatedDurationMonths.HasValue)
        {
            prompt.AppendLine($"**Estimated Completion:** {userClass.EstimatedDurationMonths} months");
        }
        if (userClass.SkillFocusAreas != null && userClass.SkillFocusAreas.Any())
        {
            prompt.AppendLine($"**Core Skill Focus Areas:** {string.Join(", ", userClass.SkillFocusAreas)}");
            prompt.AppendLine("_(Activities should prioritize and reinforce these skill areas)_");
        }
        prompt.AppendLine();

        // Calculate overall GPA
        var completedSubjects = semesterSubjects
            .Where(ss => ss.Status == SubjectEnrollmentStatus.Passed)
            .ToList();

        if (completedSubjects.Any())
        {
            double totalWeightedGrade = 0;
            int totalCredits = 0;

            foreach (var ss in completedSubjects)
            {
                if (double.TryParse(ss.Grade, out var grade) && allSubjects.TryGetValue(ss.SubjectId, out var subject))
                {
                    totalWeightedGrade += grade * subject.Credits;
                    totalCredits += subject.Credits;
                }
            }

            var overallGpa = totalCredits > 0 ? Math.Round(totalWeightedGrade / totalCredits, 2) : 0;
            prompt.AppendLine($"**Overall GPA:** {overallGpa:F2}");
            prompt.AppendLine($"**Total Credits Earned:** {totalCredits}");
            prompt.AppendLine();
        }

        // Group subjects by status
        var subjectsByStatus = semesterSubjects
            .GroupBy(ss => ss.Status)
            .OrderBy(g => GetStatusPriority(g.Key))
            .ToList();

        prompt.AppendLine("### Subjects by Status");
        prompt.AppendLine();

        foreach (var statusGroup in subjectsByStatus)
        {
            var statusName = GetStatusDisplayName(statusGroup.Key);
            prompt.AppendLine($"#### {statusName} ({statusGroup.Count()})");
            prompt.AppendLine();

            foreach (var semesterSubject in statusGroup.OrderBy(ss =>
                allSubjects.TryGetValue(ss.SubjectId, out var s) ? s.Semester : 0))
            {
                if (allSubjects.TryGetValue(semesterSubject.SubjectId, out var subject))
                {
                    var subjectGpa = double.TryParse(semesterSubject.Grade, out var grade)
                        ? $"{grade:F2}"
                        : "N/A";

                    prompt.AppendLine($"- **{subject.SubjectCode}** - {subject.SubjectName}");
                    prompt.AppendLine($"  - Semester: {subject.Semester}");
                    prompt.AppendLine($"  - Credits: {subject.Credits}");
                    prompt.AppendLine($"  - Grade: {semesterSubject.Grade ?? "N/A"}");
                    prompt.AppendLine($"  - GPA: {subjectGpa}");
                    prompt.AppendLine($"  - Academic Year: {semesterSubject.AcademicYear}");

                    if (semesterSubject.CompletedAt.HasValue)
                    {
                        prompt.AppendLine($"  - Completed: {semesterSubject.CompletedAt.Value:yyyy-MM-dd}");
                    }

                    prompt.AppendLine();
                }
            }
        }

        // Add statistics
        prompt.AppendLine("### Statistics");
        prompt.AppendLine();
        prompt.AppendLine($"- **Total Subjects:** {semesterSubjects.Count}");
        prompt.AppendLine($"- **Passed:** {semesterSubjects.Count(ss => ss.Status == SubjectEnrollmentStatus.Passed)}");
        prompt.AppendLine($"- **Currently Studying:** {semesterSubjects.Count(ss => ss.Status == SubjectEnrollmentStatus.Studying)}");
        prompt.AppendLine($"- **Not Passed:** {semesterSubjects.Count(ss => ss.Status == SubjectEnrollmentStatus.NotPassed)}");
        prompt.AppendLine($"- **Not Started:** {semesterSubjects.Count(ss => ss.Status == SubjectEnrollmentStatus.NotStarted)}");
        prompt.AppendLine($"- **Withdrawn:** {semesterSubjects.Count(ss => ss.Status == SubjectEnrollmentStatus.Withdrawn)}");

        return prompt.ToString();
    }

    private static int GetStatusPriority(SubjectEnrollmentStatus status)
    {
        return status switch
        {
            SubjectEnrollmentStatus.Studying => 1,
            SubjectEnrollmentStatus.Passed => 2,
            SubjectEnrollmentStatus.NotStarted => 3,
            SubjectEnrollmentStatus.NotPassed => 4,
            SubjectEnrollmentStatus.Withdrawn => 5,
            _ => 6
        };
    }

    private static string GetStatusDisplayName(SubjectEnrollmentStatus status)
    {
        return status switch
        {
            SubjectEnrollmentStatus.Studying => "Currently Studying",
            SubjectEnrollmentStatus.Passed => "Passed",
            SubjectEnrollmentStatus.NotStarted => "Not Started",
            SubjectEnrollmentStatus.NotPassed => "Failed",
            SubjectEnrollmentStatus.Withdrawn => "Withdrawn",
            _ => status.ToString()
        };
    }
}
