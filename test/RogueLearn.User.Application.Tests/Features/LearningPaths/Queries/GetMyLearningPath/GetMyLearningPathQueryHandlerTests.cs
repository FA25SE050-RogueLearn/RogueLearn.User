using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.LearningPaths.Queries.GetMyLearningPath;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Tests.Features.LearningPaths.Queries.GetMyLearningPath;

public class GetMyLearningPathQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoLearningPath_ReturnsNull()
    {
        var lpRepo = Substitute.For<ILearningPathRepository>();
        var chRepo = Substitute.For<IQuestChapterRepository>();
        var qRepo = Substitute.For<IQuestRepository>();
        var logger = Substitute.For<ILogger<GetMyLearningPathQueryHandler>>();

        var sut = new GetMyLearningPathQueryHandler(lpRepo, chRepo, qRepo, logger);
        var res = await sut.Handle(new GetMyLearningPathQuery { AuthUserId = Guid.NewGuid() }, CancellationToken.None);
        res.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NoChapters_ReturnsEmptyDto()
    {
        var lpRepo = Substitute.For<ILearningPathRepository>();
        var chRepo = Substitute.For<IQuestChapterRepository>();
        var qRepo = Substitute.For<IQuestRepository>();
        var logger = Substitute.For<ILogger<GetMyLearningPathQueryHandler>>();

        var lp = new LearningPath { Id = Guid.NewGuid(), Name = "LP", Description = "D" };
        lpRepo.GetLatestByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(lp);
        chRepo.GetChaptersByLearningPathIdOrderedAsync(lp.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<QuestChapter>());

        var sut = new GetMyLearningPathQueryHandler(lpRepo, chRepo, qRepo, logger);
        var res = await sut.Handle(new GetMyLearningPathQuery { AuthUserId = Guid.NewGuid() }, CancellationToken.None);
        res.Should().NotBeNull();
        res!.Chapters.Should().BeEmpty();
        res.CompletionPercentage.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithChaptersAndQuests_ComputesStatusesAndCompletion()
    {
        var lpRepo = Substitute.For<ILearningPathRepository>();
        var chRepo = Substitute.For<IQuestChapterRepository>();
        var qRepo = Substitute.For<IQuestRepository>();
        var logger = Substitute.For<ILogger<GetMyLearningPathQueryHandler>>();

        var lp = new LearningPath { Id = Guid.NewGuid(), Name = "LP", Description = "D" };
        lpRepo.GetLatestByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(lp);

        var ch1 = new QuestChapter { Id = Guid.NewGuid(), Title = "C1", Sequence = 1 };
        var ch2 = new QuestChapter { Id = Guid.NewGuid(), Title = "C2", Sequence = 2 };
        chRepo.GetChaptersByLearningPathIdOrderedAsync(lp.Id, Arg.Any<CancellationToken>()).Returns(new[] { ch1, ch2 });

        var q1 = new Quest { Id = Guid.NewGuid(), Title = "Q1", QuestChapterId = ch1.Id, Status = QuestStatus.Completed, Sequence = 2 };
        var q2 = new Quest { Id = Guid.NewGuid(), Title = "Q2", QuestChapterId = ch1.Id, Status = QuestStatus.InProgress, Sequence = 1 };
        var q3 = new Quest { Id = Guid.NewGuid(), Title = "Q3", QuestChapterId = ch2.Id, Status = QuestStatus.NotStarted, Sequence = 1 };

        qRepo.GetQuestsByChapterIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { q1, q2, q3 });

        var sut = new GetMyLearningPathQueryHandler(lpRepo, chRepo, qRepo, logger);
        var res = await sut.Handle(new GetMyLearningPathQuery { AuthUserId = Guid.NewGuid() }, CancellationToken.None);

        res.Should().NotBeNull();
        res!.CompletionPercentage.Should().Be(33.33);
        var c1 = res.Chapters.First(c => c.Id == ch1.Id);
        c1.Status.Should().Be(PathProgressStatus.InProgress.ToString());
        c1.Quests.Select(q => q.SequenceOrder).Should().BeInAscendingOrder();
        var c2 = res.Chapters.First(c => c.Id == ch2.Id);
        c2.Status.Should().Be(PathProgressStatus.NotStarted.ToString());
    }

    [Fact]
    public async Task Handle_AllCompleted_SetsChapterStatusCompleted()
    {
        var lpRepo = Substitute.For<ILearningPathRepository>();
        var chRepo = Substitute.For<IQuestChapterRepository>();
        var qRepo = Substitute.For<IQuestRepository>();
        var logger = Substitute.For<ILogger<GetMyLearningPathQueryHandler>>();

        var lp = new LearningPath { Id = Guid.NewGuid(), Name = "LP", Description = "D" };
        lpRepo.GetLatestByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(lp);

        var ch = new QuestChapter { Id = Guid.NewGuid(), Title = "C", Sequence = 1 };
        chRepo.GetChaptersByLearningPathIdOrderedAsync(lp.Id, Arg.Any<CancellationToken>()).Returns(new[] { ch });

        var q1 = new Quest { Id = Guid.NewGuid(), Title = "Q1", QuestChapterId = ch.Id, Status = QuestStatus.Completed, Sequence = 1 };
        var q2 = new Quest { Id = Guid.NewGuid(), Title = "Q2", QuestChapterId = ch.Id, Status = QuestStatus.Completed, Sequence = 2 };
        qRepo.GetQuestsByChapterIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { q1, q2 });

        var sut = new GetMyLearningPathQueryHandler(lpRepo, chRepo, qRepo, logger);
        var res = await sut.Handle(new GetMyLearningPathQuery { AuthUserId = Guid.NewGuid() }, CancellationToken.None);

        res.Should().NotBeNull();
        var chapterDto = res!.Chapters.Single();
        chapterDto.Status.Should().Be(PathProgressStatus.Completed.ToString());
        chapterDto.Quests.Select(q => q.SequenceOrder).Should().BeInAscendingOrder();
    }
}
