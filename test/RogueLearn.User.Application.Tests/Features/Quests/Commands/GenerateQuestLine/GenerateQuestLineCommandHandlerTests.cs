using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
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
        var difficultyResolver = Substitute.For<IQuestDifficultyResolver>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GenerateQuestLineCommandHandler>>();

        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, learningPathRepo, questChapterRepo, questRepo, difficultyResolver, logger);
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
        var difficultyResolver = Substitute.For<IQuestDifficultyResolver>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GenerateQuestLineCommandHandler>>();

        var authId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = Guid.NewGuid(), ClassId = Guid.NewGuid(), Username = "u" };
        userRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        subjectRepo.GetSubjectsByRoute(profile.RouteId!.Value, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        classSpecRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<ClassSpecializationSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<ClassSpecializationSubject>());
        studentSemRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<StudentSemesterSubject>());
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Quest>());
        questChapterRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<QuestChapter, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<QuestChapter>());
        learningPathRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<LearningPath, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<LearningPath>());
        learningPathRepo.AddAsync(Arg.Any<LearningPath>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<LearningPath>());

        var sut = new GenerateQuestLineCommandHandler(userRepo, subjectRepo, classSpecRepo, studentSemRepo, learningPathRepo, questChapterRepo, questRepo, difficultyResolver, logger);
        var res = await sut.Handle(new RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum.GenerateQuestLine { AuthUserId = authId }, CancellationToken.None);
        res.LearningPathId.Should().NotBeEmpty();
    }
}