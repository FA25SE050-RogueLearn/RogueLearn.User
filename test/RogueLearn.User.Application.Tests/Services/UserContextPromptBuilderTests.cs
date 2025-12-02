using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Services;

public class UserContextPromptBuilderTests
{
    private static UserContextPromptBuilder CreateSut(
        IStudentSemesterSubjectRepository? semesterRepo = null,
        ISubjectRepository? subjectRepo = null)
    {
        semesterRepo ??= Substitute.For<IStudentSemesterSubjectRepository>();
        subjectRepo ??= Substitute.For<ISubjectRepository>();
        return new UserContextPromptBuilder(semesterRepo, subjectRepo);
    }

    [Fact]
    public async Task GenerateAsync_NoRecords_ReturnsNoAcademicMessage()
    {
        var authId = Guid.NewGuid();
        var user = new UserProfile { AuthUserId = authId, Username = "u" };
        var @class = new Class { Name = "C" };
        var context = new AcademicContext();

        var semesterRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semesterRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<StudentSemesterSubject>());

        var sut = CreateSut(semesterRepo, Substitute.For<ISubjectRepository>());
        var res = await sut.GenerateAsync(user, @class, context, CancellationToken.None);
        res.Should().Contain("No academic records");
    }

    [Fact]
    public async Task GenerateAsync_WithRecords_ComputesGpa_AndFormatsSections()
    {
        var authId = Guid.NewGuid();
        var user = new UserProfile { AuthUserId = authId, Username = "student", FirstName = "A", LastName = "B", Level = 3, ExperiencePoints = 120 };
        var @class = new Class { Name = "Intro CS", Description = "desc", DifficultyLevel = DifficultyLevel.Beginner, EstimatedDurationMonths = 4, SkillFocusAreas = new[] { "Algorithms", "Data Structures" } };
        var context = new AcademicContext { CurrentGpa = 3.2 };

        var subjA = new Subject { Id = Guid.NewGuid(), SubjectCode = "CS101", SubjectName = "Intro", Credits = 3, Semester = 1 };
        var subjB = new Subject { Id = Guid.NewGuid(), SubjectCode = "CS102", SubjectName = "Algo", Credits = 4, Semester = 2 };
        var subjC = new Subject { Id = Guid.NewGuid(), SubjectCode = "CS103", SubjectName = "Net", Credits = 2, Semester = 3 };

        var s1 = new StudentSemesterSubject { AuthUserId = authId, SubjectId = subjA.Id, AcademicYear = "2024-2025", Status = SubjectEnrollmentStatus.Passed, Grade = "3.5", CompletedAt = new DateTimeOffset(2025, 5, 1, 0, 0, 0, TimeSpan.Zero) };
        var s2 = new StudentSemesterSubject { AuthUserId = authId, SubjectId = subjB.Id, AcademicYear = "2024-2025", Status = SubjectEnrollmentStatus.Passed, Grade = "2.0" };
        var s3 = new StudentSemesterSubject { AuthUserId = authId, SubjectId = subjC.Id, AcademicYear = "2024-2025", Status = SubjectEnrollmentStatus.Studying };

        var semesterRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semesterRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { s1, s2, s3 });

        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Subject> { subjA, subjB, subjC });

        var sut = CreateSut(semesterRepo, subjectRepo);
        var res = await sut.GenerateAsync(user, @class, context, CancellationToken.None);

        res.Should().Contain("Student Performance Summary");
        res.Should().Contain("**Student:** A B (student)");
        res.Should().Contain("Class Information");
        res.Should().Contain("Intro CS");
        res.Should().Contain("**Skill Focus Areas:** Algorithms, Data Structures");

        res.Should().Contain("**Overall GPA:** 2.64");
        res.Should().Contain("**Total Credits Earned:** 7");

        res.Should().Contain("Subjects by Status");
        res.Should().Contain("Currently Studying (1)");
        res.Should().Contain("Passed (2)");

        res.Should().Contain("CS101");
        res.Should().Contain("Semester: 1");
        res.Should().Contain("Credits: 3");
        res.Should().Contain("Grade: 3.5");
        res.Should().Contain("GPA: 3.50");

        res.Should().Contain("Completed: 2025-05-01");
    }
}