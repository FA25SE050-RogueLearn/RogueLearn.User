using FluentAssertions;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Tests.Models;

public class AcademicContextTests
{
    [Fact]
    public void AcademicContext_Allows_Setting_Properties()
    {
        var ctx = new AcademicContext
        {
            CurrentGpa = 3.2,
            AttemptReason = QuestAttemptReason.Retake,
            PreviousAttempts = 1,
        };
        ctx.PrerequisiteHistory.Add(new PrerequisitePerformance { SubjectCode = "MTH101", SubjectName = "Math", Grade = "B", Status = SubjectEnrollmentStatus.Passed, PerformanceLevel = "Strong" });
        ctx.RelatedSubjects.Add(new RelatedSubjectGrade { SubjectCode = "PHY101", SubjectName = "Physics", Grade = "A", NumericGrade = 9.0 });
        ctx.StrengthAreas.Add("Algorithms");
        ctx.ImprovementAreas.Add("Databases");
        ctx.CurrentGpa.Should().Be(3.2);
        ctx.PrerequisiteHistory.Count.Should().Be(1);
        ctx.RelatedSubjects.Count.Should().Be(1);
    }
}