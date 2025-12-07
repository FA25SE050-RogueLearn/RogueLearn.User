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

        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, learningPathRepo, questChapterRepo, questRepo, difficultyResolverImpl, logger);
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
        learningPathRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<LearningPath, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<LearningPath>());
        learningPathRepo.AddAsync(Arg.Any<LearningPath>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<LearningPath>());

        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, learningPathRepo, questChapterRepo, questRepo, difficultyResolverImpl, logger);
        var res = await sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = authId }, CancellationToken.None);
        res.LearningPathId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_CreatesQuest_WithRecommendationForFailedSubject()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var classSpecRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var studentSemRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var learningPathRepo = Substitute.For<ILearningPathRepository>();
        var questChapterRepo = Substitute.For<IQuestChapterRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var difficultyResolver = new QuestDifficultyResolver();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GenerateQuestLineCommandHandler>>();

        var authId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = Guid.NewGuid(), ClassId = Guid.NewGuid(), Username = "u" };
        userRepo.GetByAuthIdAsync(authId).Returns(profile);

        var subject = new Subject { Id = Guid.NewGuid(), SubjectCode = "MAT101", SubjectName = "Math", Semester = 2 };
        subjectRepo.GetSubjectsByRoute(profile.RouteId!.Value, Arg.Any<CancellationToken>()).Returns(new List<Subject> { subject });

        classSpecRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<ClassSpecializationSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ClassSpecializationSubject>());

        var learningPath = new LearningPath { Id = Guid.NewGuid(), CreatedBy = authId };
        learningPathRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<LearningPath, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<LearningPath>());
        learningPathRepo.AddAsync(Arg.Any<LearningPath>(), Arg.Any<CancellationToken>()).Returns(new LearningPath { Id = learningPath.Id, CreatedBy = authId });

        questChapterRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<QuestChapter, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<QuestChapter>());
        questChapterRepo.AddAsync(Arg.Any<QuestChapter>(), Arg.Any<CancellationToken>()).Returns(ci => { var ch = ci.Arg<QuestChapter>(); ch.Id = Guid.NewGuid(); return ch; });

        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Quest>());

        studentSemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<StudentSemesterSubject> { new() { AuthUserId = authId, SubjectId = subject.Id, Status = SubjectEnrollmentStatus.NotPassed, Grade = "4.0" } });

        // real resolver used

        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, learningPathRepo, questChapterRepo, questRepo, difficultyResolver, logger);
        await sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = authId }, CancellationToken.None);

        await questRepo.Received(1).AddAsync(Arg.Is<Quest>(q => q.SubjectId == subject.Id && q.IsRecommended && q.RecommendationReason == "Failed"), Arg.Any<CancellationToken>());
        await questChapterRepo.Received(1).AddAsync(Arg.Is<QuestChapter>(c => c.Title.StartsWith("Semester ")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ArchivesQuests_NoLongerInCurriculum()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var classSpecRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var studentSemRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var learningPathRepo = Substitute.For<ILearningPathRepository>();
        var questChapterRepo = Substitute.For<IQuestChapterRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var difficultyResolver = new QuestDifficultyResolver();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GenerateQuestLineCommandHandler>>();

        var authId = Guid.NewGuid();
        var lp = new LearningPath { Id = Guid.NewGuid(), CreatedBy = authId };
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = Guid.NewGuid(), ClassId = Guid.NewGuid(), Username = "u" };
        userRepo.GetByAuthIdAsync(authId).Returns(profile);
        learningPathRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<LearningPath, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { lp });

        var chapter = new QuestChapter { Id = Guid.NewGuid(), LearningPathId = lp.Id, Title = "Semester 1" };
        questChapterRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<QuestChapter, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { chapter });

        var oldSubjectId = Guid.NewGuid();
        var currentQuest = new Quest { Id = Guid.NewGuid(), QuestChapterId = chapter.Id, SubjectId = oldSubjectId, IsActive = true };
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { currentQuest });

        // Ideal subjects exclude oldSubjectId -> triggers archiving
        var newSubjectId = Guid.NewGuid();
        subjectRepo.GetSubjectsByRoute(profile.RouteId!.Value, Arg.Any<CancellationToken>())
            .Returns(new[] { new Subject { Id = newSubjectId, SubjectCode = "NEW" } });
        classSpecRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<ClassSpecializationSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ClassSpecializationSubject>());
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Subject { Id = newSubjectId, SubjectCode = "NEW" } });

        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, learningPathRepo, questChapterRepo, questRepo, difficultyResolver, logger);
        await sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = authId }, CancellationToken.None);

        await questRepo.Received(1).UpdateAsync(Arg.Is<Quest>(q => q.Id == currentQuest.Id && q.IsActive == false), Arg.Any<CancellationToken>());
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

        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, learningPathRepo, questChapterRepo, questRepo, difficultyResolver, logger);
        await sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = authId }, CancellationToken.None);

        await questRepo.DidNotReceive().AddAsync(Arg.Any<Quest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassedSubject_NotRecommended()
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

        var subject = new Subject { Id = Guid.NewGuid(), SubjectCode = "MAT101", SubjectName = "Math", Semester = 2 };
        subjectRepo.GetSubjectsByRoute(profile.RouteId!.Value, Arg.Any<CancellationToken>()).Returns(new[] { subject });
        classSpecRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<ClassSpecializationSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<ClassSpecializationSubject>());
        var lpId = Guid.NewGuid();
        learningPathRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<LearningPath, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new LearningPath { Id = lpId, CreatedBy = authId } });
        learningPathRepo.AddAsync(Arg.Any<LearningPath>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<LearningPath>());
        questChapterRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<QuestChapter, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new QuestChapter { Id = Guid.NewGuid(), LearningPathId = lpId, Title = "Semester 2" } });
        questChapterRepo.AddAsync(Arg.Any<QuestChapter>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<QuestChapter>());
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Quest>());

        studentSemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<StudentSemesterSubject> { new() { AuthUserId = authId, SubjectId = subject.Id, Status = SubjectEnrollmentStatus.Passed, Grade = "8.0" } });

        difficultyResolver.ResolveDifficulty(Arg.Any<StudentSemesterSubject>()).Returns(new QuestDifficultyInfo("Easy", "", "8.0", "Passed"));

        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, learningPathRepo, questChapterRepo, questRepo, difficultyResolver, logger);
        await sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = authId }, CancellationToken.None);

        await questRepo.Received(1).AddAsync(Arg.Is<Quest>(q => q.SubjectId == subject.Id && q.IsRecommended == false && q.RecommendationReason == "Passed"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdatesExistingQuest_WithNewSequence_AndRecommendation()
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
        var lpId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = Guid.NewGuid(), ClassId = Guid.NewGuid(), Username = "u" };
        userRepo.GetByAuthIdAsync(authId).Returns(profile);

        var subject = new Subject { Id = Guid.NewGuid(), SubjectCode = "MAT101", SubjectName = "Math", Semester = 1 };
        subjectRepo.GetSubjectsByRoute(profile.RouteId!.Value, Arg.Any<CancellationToken>()).Returns(new[] { subject });
        classSpecRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<ClassSpecializationSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<ClassSpecializationSubject>());
        studentSemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new List<StudentSemesterSubject> { new() { AuthUserId = authId, SubjectId = subject.Id, Status = SubjectEnrollmentStatus.Studying } });
        difficultyResolver.ResolveDifficulty(Arg.Any<StudentSemesterSubject>()).Returns(new QuestDifficultyInfo("Standard", "", null, "Studying"));

        learningPathRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<LearningPath, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[] { new LearningPath { Id = lpId, CreatedBy = authId } });

        var chapter = new QuestChapter { Id = Guid.NewGuid(), LearningPathId = lpId, Title = "Semester 1" };
        questChapterRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<QuestChapter, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[] { chapter });

        var existing = new Quest
        {
            Id = Guid.NewGuid(),
            QuestChapterId = chapter.Id,
            SubjectId = subject.Id,
            Sequence = 99,
            IsActive = false,
            IsRecommended = false,
            RecommendationReason = "Passed"
        };
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { existing });

        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, learningPathRepo, questChapterRepo, questRepo, difficultyResolver, logger);
        await sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = authId }, CancellationToken.None);

        await questRepo.Received(1).UpdateAsync(Arg.Is<Quest>(q => q.Id == existing.Id && q.Sequence == 1 && q.IsActive && q.IsRecommended && q.RecommendationReason == "Studying"), Arg.Any<CancellationToken>());
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
        learningPathRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<LearningPath, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<LearningPath>());
        learningPathRepo.AddAsync(Arg.Any<LearningPath>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<LearningPath>());
        questChapterRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<QuestChapter, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<QuestChapter>());
        studentSemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new List<StudentSemesterSubject>());
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Quest>());

        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, learningPathRepo, questChapterRepo, questRepo, difficultyResolver, logger);
        await sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = authId }, CancellationToken.None);

        await questRepo.DidNotReceive().AddAsync(Arg.Any<Quest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ClassSemesterOverride_CreatesSemesterChapter()
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

        var subjectId = Guid.NewGuid();
        classSpecRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<ClassSpecializationSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new ClassSpecializationSubject { ClassId = profile.ClassId!.Value, SubjectId = subjectId, Semester = 5 } });

        subjectRepo.GetSubjectsByRoute(profile.RouteId!.Value, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Subject { Id = subjectId, SubjectCode = "CS101", SubjectName = "CS" } });

        var lpId = Guid.NewGuid();
        learningPathRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<LearningPath, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[] { new LearningPath { Id = lpId, CreatedBy = authId } });
        questChapterRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<QuestChapter, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<QuestChapter>());
        difficultyResolver.ResolveDifficulty(Arg.Any<StudentSemesterSubject>()).Returns(new QuestDifficultyInfo("Standard", "", null, "NotStarted"));
        studentSemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new List<StudentSemesterSubject>());
        questChapterRepo.AddAsync(Arg.Any<QuestChapter>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<QuestChapter>());
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Quest>());

        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, learningPathRepo, questChapterRepo, questRepo, difficultyResolver, logger);
        await sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = authId }, CancellationToken.None);

        await questChapterRepo.Received(1).AddAsync(Arg.Is<QuestChapter>(c => c.Title == "Semester 5" && c.Sequence == 5 && c.LearningPathId == lpId), Arg.Any<CancellationToken>());
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
        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, lpRepo, chapterRepo, questRepo, diff, logger);

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
        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, lpRepo, chapterRepo, questRepo, diff, logger);

        var user = new UserProfile { AuthUserId = Guid.NewGuid(), Username = "u", RouteId = Guid.NewGuid(), ClassId = null };
        userRepo.GetByAuthIdAsync(user.AuthUserId).Returns(user);

        var act = () => sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = user.AuthUserId }, CancellationToken.None);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(act);
    }
}
