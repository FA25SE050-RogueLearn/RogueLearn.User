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
        var learningPathRepo = Substitute.For<ILearningPathRepository>();
        var questChapterRepo = Substitute.For<IQuestChapterRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var difficultyResolverImpl = new QuestDifficultyResolver();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GenerateQuestLineCommandHandler>>();

        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, learningPathRepo, questChapterRepo, questRepo, Substitute.For<IUserQuestAttemptRepository>(), difficultyResolverImpl, logger);
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
        var learningPathRepo = Substitute.For<ILearningPathRepository>();
        var questChapterRepo = Substitute.For<IQuestChapterRepository>();
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
        questChapterRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<QuestChapter, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<QuestChapter>());
        learningPathRepo.GetLatestByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(new LearningPath { Id = Guid.NewGuid(), CreatedBy = authId });

        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, learningPathRepo, questChapterRepo, questRepo, Substitute.For<IUserQuestAttemptRepository>(), difficultyResolverImpl, logger);
        var res = await sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = authId }, CancellationToken.None);
        res.LearningPathId.Should().NotBeEmpty();
    }



    [Fact]
    public async Task Handle_Filters_Excluded_SubjectCodes()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var classSpecRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var studentSemRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var learningPathRepo = Substitute.For<ILearningPathRepository>();
        var questChapterRepo = Substitute.For<IQuestChapterRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var difficultyResolver = Substitute.For<IQuestDifficultyResolver>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GenerateQuestLineCommandHandler>>();

        var authId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = Guid.NewGuid(), ClassId = Guid.NewGuid(), Username = "u" };
        userRepo.GetByAuthIdAsync(authId).Returns(profile);

        var excluded = new Subject { Id = Guid.NewGuid(), SubjectCode = "VOV114", SubjectName = "Vovinam" };
        subjectRepo.GetSubjectsByRoute(profile.RouteId!.Value, Arg.Any<CancellationToken>()).Returns(new[] { excluded });
        classSpecRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<ClassSpecializationSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<ClassSpecializationSubject>());
        learningPathRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<LearningPath, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<LearningPath>());
        learningPathRepo.AddAsync(Arg.Any<LearningPath>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<LearningPath>());
        questChapterRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<QuestChapter, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<QuestChapter>());
        studentSemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new List<StudentSemesterSubject>());
        questChapterRepo.AddAsync(Arg.Any<QuestChapter>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<QuestChapter>());
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Quest>());

        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, learningPathRepo, questChapterRepo, questRepo, Substitute.For<IUserQuestAttemptRepository>(), difficultyResolver, logger);
        await sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = authId }, CancellationToken.None);

        await questRepo.DidNotReceive().AddAsync(Arg.Any<Quest>(), Arg.Any<CancellationToken>());
    }



    [Fact]
    public async Task Handle_ExcludedByName_Orientation_NotCreated()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var classSpecRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var studentSemRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var learningPathRepo = Substitute.For<ILearningPathRepository>();
        var questChapterRepo = Substitute.For<IQuestChapterRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var difficultyResolver = Substitute.For<IQuestDifficultyResolver>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GenerateQuestLineCommandHandler>>();

        var authId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = Guid.NewGuid(), ClassId = Guid.NewGuid(), Username = "u" };
        userRepo.GetByAuthIdAsync(authId).Returns(profile);

        subjectRepo.GetSubjectsByRoute(profile.RouteId!.Value, Arg.Any<CancellationToken>()).Returns(new[] { new Subject { Id = Guid.NewGuid(), SubjectCode = "ORI101", SubjectName = "Orientation Basics", Semester = 1 } });
        classSpecRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<ClassSpecializationSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<ClassSpecializationSubject>());
        learningPathRepo.GetLatestByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(new LearningPath { Id = Guid.NewGuid(), CreatedBy = authId });
        questChapterRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<QuestChapter, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<QuestChapter>());
        studentSemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new List<StudentSemesterSubject>());
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Quest>());

        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, learningPathRepo, questChapterRepo, questRepo, Substitute.For<IUserQuestAttemptRepository>(), difficultyResolver, logger);
        await sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = authId }, CancellationToken.None);

        await questRepo.DidNotReceive().AddAsync(Arg.Any<Quest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoRoute_Throws()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var classSpecRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var studentSemRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var lpRepo = Substitute.For<ILearningPathRepository>();
        var chapterRepo = Substitute.For<IQuestChapterRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var diff = Substitute.For<IQuestDifficultyResolver>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GenerateQuestLineCommandHandler>>();
        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, lpRepo, chapterRepo, questRepo, Substitute.For<IUserQuestAttemptRepository>(), diff, logger);

        var user = new UserProfile { AuthUserId = Guid.NewGuid(), Username = "u", RouteId = null, ClassId = Guid.NewGuid() };
        userRepo.GetByAuthIdAsync(user.AuthUserId).Returns(user);

        var act = () => sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = user.AuthUserId }, CancellationToken.None);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(act);
    }

    [Fact]
    public async Task Handle_NoClass_Throws()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var classSpecRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var studentSemRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var lpRepo = Substitute.For<ILearningPathRepository>();
        var chapterRepo = Substitute.For<IQuestChapterRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var diff = Substitute.For<IQuestDifficultyResolver>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GenerateQuestLineCommandHandler>>();
        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, lpRepo, chapterRepo, questRepo, Substitute.For<IUserQuestAttemptRepository>(), diff, logger);

        var user = new UserProfile { AuthUserId = Guid.NewGuid(), Username = "u", RouteId = Guid.NewGuid(), ClassId = null };
        userRepo.GetByAuthIdAsync(user.AuthUserId).Returns(user);

        var act = () => sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = user.AuthUserId }, CancellationToken.None);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(act);
    }

    [Fact]
    public async Task Handle_CreatesAttempt_AssignsAdaptiveDifficultyAndNotes()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var classSpecRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var studentSemRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var learningPathRepo = Substitute.For<ILearningPathRepository>();
        var questChapterRepo = Substitute.For<IQuestChapterRepository>();
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

        learningPathRepo.GetLatestByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(new LearningPath { Id = Guid.NewGuid(), CreatedBy = authId });

        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);

        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, learningPathRepo, questChapterRepo, questRepo, attemptRepo, difficultyResolver, logger);
        var result = await sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = authId }, CancellationToken.None);

        result.LearningPathId.Should().NotBeEmpty();
        await attemptRepo.Received(1).AddAsync(
            Arg.Is<UserQuestAttempt>(a => a.AuthUserId == authId && a.QuestId == masterQuest.Id && a.AssignedDifficulty == "Adaptive" && a.Notes == "Currently enrolled - content adapts to your progress"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdatesExistingAttempt_WhenDifficultyChanges()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var classSpecRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var studentSemRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var learningPathRepo = Substitute.For<ILearningPathRepository>();
        var questChapterRepo = Substitute.For<IQuestChapterRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var difficultyResolver = new QuestDifficultyResolver();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GenerateQuestLineCommandHandler>>();

        var authId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = Guid.NewGuid(), ClassId = Guid.NewGuid(), Username = "user" };
        userRepo.GetByAuthIdAsync(authId).Returns(profile);

        var subject = new Subject { Id = Guid.NewGuid(), SubjectCode = "PHY101", SubjectName = "Physics" };
        subjectRepo.GetSubjectsByRoute(profile.RouteId!.Value, Arg.Any<CancellationToken>()).Returns(new[] { subject });
        classSpecRepo.GetSubjectByClassIdAsync(profile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());

        var masterQuest = new Quest { Id = Guid.NewGuid(), SubjectId = subject.Id, IsActive = true };
        questRepo.GetActiveQuestBySubjectIdAsync(subject.Id, Arg.Any<CancellationToken>()).Returns(masterQuest);

        var gradeRecord = new StudentSemesterSubject { AuthUserId = authId, SubjectId = subject.Id, Status = SubjectEnrollmentStatus.NotPassed, Grade = "5.0" };
        studentSemRepo.GetSemesterSubjectsByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(new List<StudentSemesterSubject> { gradeRecord });

        learningPathRepo.GetLatestByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(new LearningPath { Id = Guid.NewGuid(), CreatedBy = authId });

        var existingAttempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = masterQuest.Id, AssignedDifficulty = "Standard" };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(existingAttempt);

        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, learningPathRepo, questChapterRepo, questRepo, attemptRepo, difficultyResolver, logger);
        await sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = authId }, CancellationToken.None);

        await attemptRepo.Received(1).UpdateAsync(
            Arg.Is<UserQuestAttempt>(a => a.Id == existingAttempt.Id && a.AssignedDifficulty == "Supportive" && a.Notes != null && a.Notes.StartsWith("Difficulty updated:")),
            Arg.Any<CancellationToken>());
        await attemptRepo.DidNotReceive().AddAsync(Arg.Any<UserQuestAttempt>(), Arg.Any<CancellationToken>());
    }
}
