using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Student.Queries.GetAcademicStatus;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Student.Queries.GetAcademicStatus;

public class GetAcademicStatusQueryHandlerTests
{
    private static GetAcademicStatusQueryHandler CreateSut(
        IStudentEnrollmentRepository? enrollRepo = null,
        IStudentSemesterSubjectRepository? semesterRepo = null,
        ISubjectRepository? subjectRepo = null,
        IClassRepository? classRepo = null,
        ILogger<GetAcademicStatusQueryHandler>? logger = null)
    {
        enrollRepo ??= Substitute.For<IStudentEnrollmentRepository>();
        semesterRepo ??= Substitute.For<IStudentSemesterSubjectRepository>();
        subjectRepo ??= Substitute.For<ISubjectRepository>();
        classRepo ??= Substitute.For<IClassRepository>();
        logger ??= Substitute.For<ILogger<GetAcademicStatusQueryHandler>>();
        return new GetAcademicStatusQueryHandler(enrollRepo, semesterRepo, subjectRepo, classRepo, logger);
    }

    [Fact]
    public async Task Handle_ReturnsNull_When_EnrollmentMissing()
    {
        var enrollRepo = Substitute.For<IStudentEnrollmentRepository>();
        enrollRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentEnrollment, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((StudentEnrollment?)null);

        var sut = CreateSut(enrollRepo: enrollRepo);
        var res = await sut.Handle(new GetAcademicStatusQuery { AuthUserId = Guid.NewGuid() }, CancellationToken.None);
        res.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NoSubjects_ReturnsZeroCounts_AndZeroGpa()
    {
        var userId = Guid.NewGuid();
        var enrollRepo = Substitute.For<IStudentEnrollmentRepository>();
        enrollRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentEnrollment, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new StudentEnrollment { Id = Guid.NewGuid(), AuthUserId = userId });

        var semesterRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semesterRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<StudentSemesterSubject>());

        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Subject>());

        var sut = CreateSut(enrollRepo: enrollRepo, semesterRepo: semesterRepo, subjectRepo: subjectRepo);
        var res = await sut.Handle(new GetAcademicStatusQuery { AuthUserId = userId }, CancellationToken.None);

        res.Should().NotBeNull();
        res!.CompletedSubjects.Should().Be(0);
        res.InProgressSubjects.Should().Be(0);
        res.FailedSubjects.Should().Be(0);
        res.CurrentGpa.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ComputesCounts_And_Gpa_UsingNumericGradesAndCredits()
    {
        var userId = Guid.NewGuid();
        var enrollRepo = Substitute.For<IStudentEnrollmentRepository>();
        enrollRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentEnrollment, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new StudentEnrollment { Id = Guid.NewGuid(), AuthUserId = userId });

        var s1 = Guid.NewGuid(); // completed, numeric grade
        var s2 = Guid.NewGuid(); // completed, non-numeric grade (ignored)
        var s3 = Guid.NewGuid(); // studying
        var s4 = Guid.NewGuid(); // failed

        var semesterRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semesterRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new StudentSemesterSubject { AuthUserId = userId, SubjectId = s1, Status = SubjectEnrollmentStatus.Passed, Grade = "3.5" },
                new StudentSemesterSubject { AuthUserId = userId, SubjectId = s2, Status = SubjectEnrollmentStatus.Passed, Grade = "A" },
                new StudentSemesterSubject { AuthUserId = userId, SubjectId = s3, Status = SubjectEnrollmentStatus.Studying, Grade = null },
                new StudentSemesterSubject { AuthUserId = userId, SubjectId = s4, Status = SubjectEnrollmentStatus.NotPassed, Grade = "2.0" },
            });

        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new Subject { Id = s1, Credits = 3 },
            new Subject { Id = s2, Credits = 4 },
            new Subject { Id = s3, Credits = 2 },
            new Subject { Id = s4, Credits = 5 },
        });

        var sut = CreateSut(enrollRepo: enrollRepo, semesterRepo: semesterRepo, subjectRepo: subjectRepo);
        var res = await sut.Handle(new GetAcademicStatusQuery { AuthUserId = userId }, CancellationToken.None);

        res.Should().NotBeNull();
        res!.CompletedSubjects.Should().Be(2);
        res.InProgressSubjects.Should().Be(1);
        res.FailedSubjects.Should().Be(1);

        // GPA uses only numeric grade from s1: 3.5 * 3 credits = 10.5; denominator 3
        res.CurrentGpa.Should().Be(3.5);
    }

    [Fact]
    public async Task Handle_CompletedWithNonNumericGrades_SetsGpaZero()
    {
        var userId = Guid.NewGuid();
        var enrollRepo = Substitute.For<IStudentEnrollmentRepository>();
        enrollRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentEnrollment, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new StudentEnrollment { Id = Guid.NewGuid(), AuthUserId = userId });

        var s1 = Guid.NewGuid();

        var semesterRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semesterRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new StudentSemesterSubject { AuthUserId = userId, SubjectId = s1, Status = SubjectEnrollmentStatus.Passed, Grade = "A" }
            });

        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new Subject { Id = s1, Credits = 3 }
        });

        var sut = CreateSut(enrollRepo: enrollRepo, semesterRepo: semesterRepo, subjectRepo: subjectRepo);
        var res = await sut.Handle(new GetAcademicStatusQuery { AuthUserId = userId }, CancellationToken.None);

        res.Should().NotBeNull();
        res!.CompletedSubjects.Should().Be(1);
        res.CurrentGpa.Should().Be(0);
    }
}
