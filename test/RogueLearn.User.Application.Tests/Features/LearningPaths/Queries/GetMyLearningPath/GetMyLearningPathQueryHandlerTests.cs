using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.LearningPaths.Queries.GetMyLearningPath;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.LearningPaths.Queries.GetMyLearningPath;

public class GetMyLearningPathQueryHandlerTests
{
    // Removed ILearningPathRepository from parameters as it was removed from the handler
    private static GetMyLearningPathQueryHandler CreateSut(
        IStudentSemesterSubjectRepository? studentRepo = null,
        ISubjectRepository? subjectRepo = null,
        IQuestRepository? questRepo = null,
        IUserQuestAttemptRepository? attemptRepo = null,
        IUserProfileRepository? userRepo = null,
        ICurriculumProgramSubjectRepository? programSubjectRepo = null,
        IClassSpecializationSubjectRepository? classSubjectRepo = null,
        IQuestDifficultyResolver? difficultyResolver = null,
        ILogger<GetMyLearningPathQueryHandler>? logger = null)
    {
        studentRepo ??= Substitute.For<IStudentSemesterSubjectRepository>();
        subjectRepo ??= Substitute.For<ISubjectRepository>();
        questRepo ??= Substitute.For<IQuestRepository>();
        attemptRepo ??= Substitute.For<IUserQuestAttemptRepository>();
        userRepo ??= Substitute.For<IUserProfileRepository>();
        programSubjectRepo ??= Substitute.For<ICurriculumProgramSubjectRepository>();
        classSubjectRepo ??= Substitute.For<IClassSpecializationSubjectRepository>();
        difficultyResolver ??= Substitute.For<IQuestDifficultyResolver>();
        logger ??= Substitute.For<ILogger<GetMyLearningPathQueryHandler>>();

        return new GetMyLearningPathQueryHandler(
            studentRepo,
            subjectRepo,
            questRepo,
            attemptRepo,
            userRepo,
            programSubjectRepo,
            classSubjectRepo,
            difficultyResolver,
            logger);
    }

    [Fact]
    public async Task NoUserProfile_ReturnsNull()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        var sut = CreateSut(userRepo: userRepo);

        var res = await sut.Handle(new GetMyLearningPathQuery { AuthUserId = Guid.NewGuid() }, CancellationToken.None);
        res.Should().BeNull();
    }

    [Fact]
    public async Task IncompleteAcademicPath_ReturnsUnassignedStub()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new UserProfile { AuthUserId = Guid.NewGuid(), RouteId = null, ClassId = null, Username = "u", FirstName = "User" });

        var sut = CreateSut(userRepo: userRepo);
        var res = await sut.Handle(new GetMyLearningPathQuery { AuthUserId = Guid.NewGuid() }, CancellationToken.None);

        res.Should().NotBeNull();
        res!.Name.Should().Be("Unassigned Path");
        res.Chapters.Should().BeEmpty();
        res.CompletionPercentage.Should().Be(0);
    }

    [Fact]
    public async Task EmptyCurriculum_ReturnsDescriptionForNoSubjects()
    {
        var authId = Guid.NewGuid();
        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>())
            .Returns(new UserProfile { AuthUserId = authId, RouteId = Guid.NewGuid(), ClassId = Guid.NewGuid(), Username = "u", FirstName = "User" });

        var programSubjectRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        programSubjectRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CurriculumProgramSubject>());

        var classSubjectRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        classSubjectRepo.GetSubjectByClassIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Subject>());

        var sut = CreateSut(userRepo: userRepo, programSubjectRepo: programSubjectRepo, classSubjectRepo: classSubjectRepo);
        var res = await sut.Handle(new GetMyLearningPathQuery { AuthUserId = authId }, CancellationToken.None);

        res.Should().NotBeNull();
        res!.Name.Should().Be("User's Journey");
        res.Description.Should().Be("No subjects found in your current curriculum.");
        res.Chapters.Should().BeEmpty();
    }

    [Fact]
    public async Task NormalFlow_GroupingStatusesAndCompletionCalculated()
    {
        var authId = Guid.NewGuid();
        var routeId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>())
            .Returns(new UserProfile { AuthUserId = authId, RouteId = routeId, ClassId = classId, Username = "user", FirstName = "My" });

        var subj1 = Guid.NewGuid();
        var subj2 = Guid.NewGuid();

        var programSubjectRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        programSubjectRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CurriculumProgramSubject> { new CurriculumProgramSubject { ProgramId = routeId, SubjectId = subj1 } });

        var classSubjectRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        classSubjectRepo.GetSubjectByClassIdAsync(classId, Arg.Any<CancellationToken>())
            .Returns(new List<Subject> { new Subject { Id = subj2, Semester = null } });

        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetByIdsAsync(Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(subj1) && ids.Contains(subj2)), Arg.Any<CancellationToken>())
            .Returns(new List<Subject> {
                new Subject { Id = subj1, SubjectName = "S1", Semester = 1 },
                new Subject { Id = subj2, SubjectName = "S2", Semester = null }
            });

        var studentRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        studentRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<StudentSemesterSubject> { new StudentSemesterSubject { AuthUserId = authId, SubjectId = subj2, Status = SubjectEnrollmentStatus.Passed, Grade = "9.0" } });

        var quest1 = new Quest { Id = Guid.NewGuid(), SubjectId = subj1, Title = "Q1", IsActive = true, ExpectedDifficulty = "Hard", DifficultyReason = "sync" };
        var quest2 = new Quest { Id = Guid.NewGuid(), SubjectId = subj2, Title = "Q2", IsActive = true };

        var questRepo = Substitute.For<IQuestRepository>();
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Quest> { quest1, quest2 });

        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        attemptRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserQuestAttempt> {
                new UserQuestAttempt { AuthUserId = authId, QuestId = quest1.Id, Status = QuestAttemptStatus.InProgress, Notes = "note" }
            });

        // Mock difficulty resolver
        var difficultyResolver = Substitute.For<IQuestDifficultyResolver>();
        difficultyResolver.ResolveDifficulty(Arg.Any<StudentSemesterSubject?>())
            .Returns(new QuestDifficultyInfo { ExpectedDifficulty = "Standard", DifficultyReason = "Reason" });

        // Removed the null first argument
        var sut = CreateSut(studentRepo, subjectRepo, questRepo, attemptRepo, userRepo, programSubjectRepo, classSubjectRepo, difficultyResolver);

        var res = await sut.Handle(new GetMyLearningPathQuery { AuthUserId = authId }, CancellationToken.None);

        res.Should().NotBeNull();
        res!.Chapters.Should().NotBeEmpty();
        res.Chapters.Select(c => c.Title).Should().Contain(new[] { "Semester 1", "Electives / Unassigned" });

        // Q1 is InProgress, Q2 has no attempt but subject is Passed -> Logic says "Completed"
        // Total quests = 2. Q2 is completed virtually. Count = 1. Percentage = 50.
        res.CompletionPercentage.Should().Be(50);

        res.Chapters.SelectMany(c => c.Quests).Should().Contain(q => q.Id == quest1.Id && q.Status == "InProgress");
        res.Chapters.SelectMany(c => c.Quests).Should().Contain(q => q.Id == quest2.Id && q.Status == "Completed");
        res.Name.Should().Be("My's Journey");
    }
}