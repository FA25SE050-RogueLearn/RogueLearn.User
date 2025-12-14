using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Quests.Commands.GenerateQuestLine;

public class GenerateQuestLineCommandHandlerTests
{
    [Fact]
    public async Task Handle_ThrowsWhenUserProfileMissingOrIncomplete()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var classSpecRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var studentSemRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var difficultyResolverImpl = new QuestDifficultyResolver();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GenerateQuestLineCommandHandler>>();

        // ILearningPathRepository and IQuestChapterRepository are removed
        var sut = new GenerateQuestLineCommandHandler(
            userRepo,
            subjectRepo,
            classSpecRepo,
            studentSemRepo,
            questRepo,
            Substitute.For<IUserQuestAttemptRepository>(),
            difficultyResolverImpl,
            logger);

        var act = () => sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = Guid.NewGuid() }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.BadRequestException>();
    }

    [Fact]
    public async Task Handle_SucceedsWithEmptySubjects()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var classSpecRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var studentSemRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var difficultyResolverImpl = new QuestDifficultyResolver();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GenerateQuestLineCommandHandler>>();

        var authId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = Guid.NewGuid(), ClassId = Guid.NewGuid(), Username = "u" };
        userRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        subjectRepo.GetSubjectsByRoute(profile.RouteId!.Value, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        classSpecRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<ClassSpecializationSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<ClassSpecializationSubject>());
        studentSemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new List<StudentSemesterSubject>());
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Quest>());

        var sut = new GenerateQuestLineCommandHandler(
            userRepo,
            subjectRepo,
            classSpecRepo,
            studentSemRepo,
            questRepo,
            Substitute.For<IUserQuestAttemptRepository>(),
            difficultyResolverImpl,
            logger);

        var res = await sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = authId }, CancellationToken.None);
        res.LearningPathId.Should().NotBeEmpty(); // Returns authId as learningPathId
    }

    [Fact]
    public async Task Handle_Filters_Excluded_SubjectCodes()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var classSpecRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var studentSemRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var difficultyResolver = Substitute.For<IQuestDifficultyResolver>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GenerateQuestLineCommandHandler>>();

        var authId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = Guid.NewGuid(), ClassId = Guid.NewGuid(), Username = "u" };
        userRepo.GetByAuthIdAsync(authId).Returns(profile);

        var excluded = new Subject { Id = Guid.NewGuid(), SubjectCode = "VOV114", SubjectName = "Vovinam" };
        subjectRepo.GetSubjectsByRoute(profile.RouteId!.Value, Arg.Any<CancellationToken>()).Returns(new[] { excluded });
        classSpecRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<ClassSpecializationSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<ClassSpecializationSubject>());
        studentSemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new List<StudentSemesterSubject>());
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Quest>());

        var sut = new GenerateQuestLineCommandHandler(
            userRepo,
            subjectRepo,
            classSpecRepo,
            studentSemRepo,
            questRepo,
            Substitute.For<IUserQuestAttemptRepository>(),
            difficultyResolver,
            logger);

        await sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = authId }, CancellationToken.None);

        await questRepo.DidNotReceive().AddAsync(Arg.Any<Quest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CreatesAttempt_AssignsNotes_BasedOnDifficulty()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var classSpecRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var studentSemRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var difficultyResolver = new QuestDifficultyResolver();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GenerateQuestLineCommandHandler>>();

        var authId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = Guid.NewGuid(), ClassId = Guid.NewGuid(), Username = "user" };
        userRepo.GetByAuthIdAsync(authId).Returns(profile);

        var subject = new Subject { Id = Guid.NewGuid(), SubjectCode = "MATH101", SubjectName = "Calculus" };
        subjectRepo.GetSubjectsByRoute(profile.RouteId!.Value, Arg.Any<CancellationToken>()).Returns(new[] { subject });
        classSpecRepo.GetSubjectByClassIdAsync(profile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());

        var masterQuest = new Quest { Id = Guid.NewGuid(), SubjectId = subject.Id, IsActive = true };
        questRepo.GetActiveQuestBySubjectIdAsync(subject.Id, Arg.Any<CancellationToken>()).Returns(masterQuest);

        var gradeRecord = new StudentSemesterSubject { AuthUserId = authId, SubjectId = subject.Id, Status = SubjectEnrollmentStatus.Studying, Grade = "8.0" };
        studentSemRepo.GetSemesterSubjectsByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(new List<StudentSemesterSubject> { gradeRecord });

        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);

        var sut = new GenerateQuestLineCommandHandler(
            userRepo,
            subjectRepo,
            classSpecRepo,
            studentSemRepo,
            questRepo,
            attemptRepo,
            difficultyResolver,
            logger);

        var result = await sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = authId }, CancellationToken.None);

        await attemptRepo.Received(1).AddAsync(
            Arg.Is<UserQuestAttempt>(a => a.AuthUserId == authId && a.QuestId == masterQuest.Id && a.Notes!.Contains("content adapts")),
            Arg.Any<CancellationToken>());
    }
}