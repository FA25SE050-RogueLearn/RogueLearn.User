using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Services;

public class AcademicContextBuilderTests
{
    private static AcademicContextBuilder CreateSut(
        IStudentSemesterSubjectRepository? semesterRepo = null,
        ISubjectRepository? subjectRepo = null,
        ILogger<AcademicContextBuilder>? logger = null)
    {
        semesterRepo ??= Substitute.For<IStudentSemesterSubjectRepository>();
        subjectRepo ??= Substitute.For<ISubjectRepository>();
        logger ??= Substitute.For<ILogger<AcademicContextBuilder>>();
        return new AcademicContextBuilder(semesterRepo, subjectRepo, logger);
    }

    [Fact]
    public async Task BuildContextAsync_TargetSubjectNotFound_ReturnsDefaultContext()
    {
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Subject?)null);

        var sut = CreateSut(subjectRepo: subjectRepo);
        var ctx = await sut.BuildContextAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        ctx.Should().NotBeNull();
        ctx.CurrentGpa.Should().Be(0);
        ctx.AttemptReason.Should().Be(QuestAttemptReason.FirstTime);
        ctx.PreviousAttempts.Should().Be(0);
        ctx.PrerequisiteHistory.Should().BeEmpty();
        ctx.RelatedSubjects.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildContextAsync_NoHistory_FirstTimeAttempt()
    {
        var authId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();

        var subject = new Subject { Id = subjectId, SubjectCode = "PRO201", SubjectName = "OOP", Semester = 2 };
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetByIdAsync(subjectId, Arg.Any<CancellationToken>()).Returns(subject);
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Subject> { subject });

        var semesterRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semesterRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<StudentSemesterSubject>());

        var sut = CreateSut(semesterRepo, subjectRepo);
        var ctx = await sut.BuildContextAsync(authId, subjectId, CancellationToken.None);

        ctx.AttemptReason.Should().Be(QuestAttemptReason.FirstTime);
        ctx.PreviousAttempts.Should().Be(0);
        ctx.CurrentGpa.Should().Be(0);
    }

    [Fact]
    public async Task BuildContextAsync_WithPassedGrades_ComputesGpa_AndAttemptReason()
    {
        var authId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var sTarget = new Subject { Id = targetId, SubjectCode = "PRO192", SubjectName = "Programming", Semester = 3 };
        var sOther = new Subject { Id = Guid.NewGuid(), SubjectCode = "PRO201", SubjectName = "OOP", Semester = 2 };

        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetByIdAsync(targetId, Arg.Any<CancellationToken>()).Returns(sTarget);
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Subject> { sTarget, sOther });

        var history = new List<StudentSemesterSubject>
        {
            new() { AuthUserId = authId, SubjectId = targetId, Status = SubjectEnrollmentStatus.NotPassed, Grade = "5", CreditsEarned = 3, AcademicYear = "2023" },
            new() { AuthUserId = authId, SubjectId = sOther.Id, Status = SubjectEnrollmentStatus.Passed, Grade = "8", CreditsEarned = 3, AcademicYear = "2024" },
            new() { AuthUserId = authId, SubjectId = sOther.Id, Status = SubjectEnrollmentStatus.Passed, Grade = "7", CreditsEarned = 4, AcademicYear = "2023" },
        };

        var semesterRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semesterRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(history);

        var sut = CreateSut(semesterRepo, subjectRepo);
        var ctx = await sut.BuildContextAsync(authId, targetId, CancellationToken.None);

        ctx.PreviousAttempts.Should().Be(1);
        ctx.AttemptReason.Should().Be(QuestAttemptReason.Retake);
        ctx.CurrentGpa.Should().BeApproximately(7.428, 0.001);
        ctx.PrerequisiteHistory.Should().NotBeEmpty();
        ctx.RelatedSubjects.Should().NotBeEmpty();
    }
}