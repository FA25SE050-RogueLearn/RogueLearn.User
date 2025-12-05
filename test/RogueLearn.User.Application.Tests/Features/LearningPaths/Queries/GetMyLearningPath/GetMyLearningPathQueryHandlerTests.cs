using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.LearningPaths.Queries.GetMyLearningPath;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.LearningPaths.Queries.GetMyLearningPath;

public class GetMyLearningPathQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoPath_ReturnsNull()
    {
        var lpRepo = Substitute.For<ILearningPathRepository>();
        var chapterRepo = Substitute.For<IQuestChapterRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var logger = Substitute.For<ILogger<GetMyLearningPathQueryHandler>>();
        var sut = new GetMyLearningPathQueryHandler(lpRepo, chapterRepo, questRepo, logger);

        var query = new GetMyLearningPathQuery { AuthUserId = System.Guid.NewGuid() };
        lpRepo.GetLatestByUserAsync(query.AuthUserId, Arg.Any<CancellationToken>()).Returns((LearningPath?)null);
        var result = await sut.Handle(query, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithChapterAndQuest_ComputesCompletion()
    {
        var lpRepo = Substitute.For<ILearningPathRepository>();
        var chapterRepo = Substitute.For<IQuestChapterRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var logger = Substitute.For<ILogger<GetMyLearningPathQueryHandler>>();
        var sut = new GetMyLearningPathQueryHandler(lpRepo, chapterRepo, questRepo, logger);

        var lp = new LearningPath { Id = System.Guid.NewGuid(), Name = "LP" };
        var chapter = new QuestChapter { Id = System.Guid.NewGuid(), LearningPathId = lp.Id, Title = "Ch1", Sequence = 1 };
        var quest = new Quest { Id = System.Guid.NewGuid(), QuestChapterId = chapter.Id, Title = "Q1", Status = QuestStatus.Completed, Sequence = 1 };

        var query = new GetMyLearningPathQuery { AuthUserId = System.Guid.NewGuid() };
        lpRepo.GetLatestByUserAsync(query.AuthUserId, Arg.Any<CancellationToken>()).Returns(lp);
        chapterRepo.GetChaptersByLearningPathIdOrderedAsync(lp.Id, Arg.Any<CancellationToken>()).Returns(new List<QuestChapter> { chapter });
        questRepo.GetQuestsByChapterIdsAsync(Arg.Is<List<System.Guid>>(ids => ids.Contains(chapter.Id)), Arg.Any<CancellationToken>()).Returns(new List<Quest> { quest });

        var result = await sut.Handle(query, CancellationToken.None);
        result!.Chapters.Should().HaveCount(1);
        result.CompletionPercentage.Should().Be(100);
    }
}