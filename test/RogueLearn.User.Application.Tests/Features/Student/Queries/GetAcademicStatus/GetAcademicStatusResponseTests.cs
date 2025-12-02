using FluentAssertions;
using RogueLearn.User.Application.Features.Student.Queries.GetAcademicStatus;

namespace RogueLearn.User.Application.Tests.Features.Student.Queries.GetAcademicStatus;

public class GetAcademicStatusResponseTests
{
    [Fact]
    public void DefaultValues_ShouldInitializeCorrectly()
    {
        var resp = new GetAcademicStatusResponse();
        resp.EnrollmentId.Should().BeNull();
        resp.CurriculumVersionId.Should().BeNull();
        resp.CurriculumProgramName.Should().Be(string.Empty);
        resp.CurrentGpa.Should().Be(0.0);
        resp.TotalSubjects.Should().Be(0);
        resp.CompletedSubjects.Should().Be(0);
        resp.InProgressSubjects.Should().Be(0);
        resp.FailedSubjects.Should().Be(0);
        resp.LearningPathId.Should().BeNull();
        resp.TotalQuests.Should().Be(0);
        resp.CompletedQuests.Should().Be(0);
        resp.SkillInitialization.Should().NotBeNull();
        resp.Subjects.Should().NotBeNull();
        resp.Chapters.Should().NotBeNull();
    }

    [Fact]
    public void Properties_ShouldBeAssignable()
    {
        var resp = new GetAcademicStatusResponse
        {
            EnrollmentId = Guid.NewGuid(),
            CurriculumVersionId = Guid.NewGuid(),
            CurriculumProgramName = "CS Program",
            CurrentGpa = 3.2,
            TotalSubjects = 20,
            CompletedSubjects = 12,
            InProgressSubjects = 5,
            FailedSubjects = 3,
            LearningPathId = Guid.NewGuid(),
            TotalQuests = 50,
            CompletedQuests = 34,
            SkillInitialization = new SkillInitializationInfo { IsInitialized = true, TotalSkills = 120, LastInitializedAt = DateTimeOffset.UtcNow },
            Subjects =
            [
                new SubjectProgressDto { SubjectId = Guid.NewGuid(), SubjectCode = "MATH101", SubjectName = "Calculus", Semester = 1, Status = "Passed", Grade = "A", QuestId = Guid.NewGuid(), QuestStatus = "Completed" }
            ],
            Chapters =
            [
                new ChapterProgressDto { ChapterId = Guid.NewGuid(), Title = "Intro", Sequence = 1, Status = "Done", TotalQuests = 10, CompletedQuests = 10 }
            ]
        };

        resp.CurriculumProgramName.Should().Be("CS Program");
        resp.CurrentGpa.Should().Be(3.2);
        resp.TotalQuests.Should().Be(50);
        resp.SkillInitialization.IsInitialized.Should().BeTrue();
        resp.Subjects.Should().HaveCount(1);
        resp.Subjects[0].SubjectCode.Should().Be("MATH101");
        resp.Chapters.Should().HaveCount(1);
        resp.Chapters[0].Title.Should().Be("Intro");
    }
}

public class SkillInitializationInfoTests
{
    [Fact]
    public void DefaultValues_ShouldInitializeCorrectly()
    {
        var info = new SkillInitializationInfo();
        info.IsInitialized.Should().BeFalse();
        info.TotalSkills.Should().Be(0);
        info.LastInitializedAt.Should().BeNull();
    }
}

public class SubjectProgressDtoTests
{
    [Fact]
    public void DefaultValues_ShouldInitializeCorrectly()
    {
        var dto = new SubjectProgressDto();
        dto.SubjectId.Should().Be(Guid.Empty);
        dto.SubjectCode.Should().Be(string.Empty);
        dto.SubjectName.Should().Be(string.Empty);
        dto.Semester.Should().Be(0);
        dto.Status.Should().Be(string.Empty);
        dto.Grade.Should().BeNull();
        dto.QuestId.Should().BeNull();
        dto.QuestStatus.Should().BeNull();
    }
}

public class ChapterProgressDtoTests
{
    [Fact]
    public void DefaultValues_ShouldInitializeCorrectly()
    {
        var dto = new ChapterProgressDto();
        dto.ChapterId.Should().Be(Guid.Empty);
        dto.Title.Should().Be(string.Empty);
        dto.Sequence.Should().Be(0);
        dto.Status.Should().Be(string.Empty);
        dto.TotalQuests.Should().Be(0);
        dto.CompletedQuests.Should().Be(0);
    }
}