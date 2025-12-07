using FluentAssertions;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Tests.Services;

public class QuestDifficultyResolverTests
{
    [Fact]
    public void ResolveDifficulty_Returns_Standard_For_Null_Record()
    {
        var resolver = new QuestDifficultyResolver();
        var info = resolver.ResolveDifficulty(null);
        info.ExpectedDifficulty.Should().Be("Standard");
        info.SubjectStatus.Should().Be("NotStarted");
    }

    [Fact]
    public void ResolveDifficulty_Studying_Is_Adaptive()
    {
        var resolver = new QuestDifficultyResolver();
        var info = resolver.ResolveDifficulty(new StudentSemesterSubject { Status = SubjectEnrollmentStatus.Studying, Grade = "8.0" });
        info.ExpectedDifficulty.Should().Be("Adaptive");
        info.SubjectStatus.Should().Be("Studying");
    }

    [Fact]
    public void ResolveDifficulty_NotPassed_Is_Supportive()
    {
        var resolver = new QuestDifficultyResolver();
        var info = resolver.ResolveDifficulty(new StudentSemesterSubject { Status = SubjectEnrollmentStatus.NotPassed, Grade = "5.0" });
        info.ExpectedDifficulty.Should().Be("Supportive");
        info.SubjectStatus.Should().Be("NotPassed");
    }

    [Fact]
    public void ResolveDifficulty_Passed_High_Is_Challenging()
    {
        var resolver = new QuestDifficultyResolver();
        var info = resolver.ResolveDifficulty(new StudentSemesterSubject { Status = SubjectEnrollmentStatus.Passed, Grade = "9.0" });
        info.ExpectedDifficulty.Should().Be("Challenging");
        info.SubjectStatus.Should().Be("Passed");
    }

    [Fact]
    public void ResolveDifficulty_Passed_Mid_Is_Standard()
    {
        var resolver = new QuestDifficultyResolver();
        var info = resolver.ResolveDifficulty(new StudentSemesterSubject { Status = SubjectEnrollmentStatus.Passed, Grade = "7.5" });
        info.ExpectedDifficulty.Should().Be("Standard");
        info.SubjectStatus.Should().Be("Passed");
    }

    [Fact]
    public void ResolveDifficulty_Passed_Low_Is_Supportive()
    {
        var resolver = new QuestDifficultyResolver();
        var info = resolver.ResolveDifficulty(new StudentSemesterSubject { Status = SubjectEnrollmentStatus.Passed, Grade = "6.0" });
        info.ExpectedDifficulty.Should().Be("Supportive");
        info.SubjectStatus.Should().Be("Passed");
    }
}