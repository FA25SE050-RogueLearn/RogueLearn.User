using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Queries.GetAllQuests;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Quests.Queries.GetAllQuests;

public class GetAllQuestsQueryHandlerTests
{
    [Fact]
    public async Task Handle_PaginatesAndOrders_ByCreatedAtDesc()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GetAllQuestsQueryHandler>>();

        var now = DateTimeOffset.UtcNow;
        var s1 = Guid.NewGuid();
        var q1 = new Quest { Id = Guid.NewGuid(), Title = "Q1", CreatedAt = now.AddMinutes(-10), SubjectId = s1, Status = QuestStatus.NotStarted };
        var q2 = new Quest { Id = Guid.NewGuid(), Title = "Q2", CreatedAt = now.AddMinutes(-5), Status = QuestStatus.InProgress };
        var q3 = new Quest { Id = Guid.NewGuid(), Title = "Q3", CreatedAt = now.AddMinutes(-1), SubjectId = s1, Status = QuestStatus.Completed };
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { q1, q2, q3 });

        subjectRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new Subject { Id = s1, SubjectCode = "SC", SubjectName = "SN" } });

        var sut = new GetAllQuestsQueryHandler(questRepo, subjectRepo, logger);
        var res = await sut.Handle(new GetAllQuestsQuery { Page = 1, PageSize = 2 }, CancellationToken.None);

        res.TotalCount.Should().Be(3);
        res.TotalPages.Should().Be(2);
        res.Items.Should().HaveCount(2);
        res.Items[0].Title.Should().Be("Q3");
        res.Items[1].Title.Should().Be("Q2");
        res.Items[0].SubjectCode.Should().Be("SC");
        res.Items[0].SubjectName.Should().Be("SN");
    }

    [Fact]
    public async Task Handle_FiltersBySearchTerm()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GetAllQuestsQueryHandler>>();

        var q1 = new Quest { Id = Guid.NewGuid(), Title = "Alpha", Description = "includes needle" };
        var q2 = new Quest { Id = Guid.NewGuid(), Title = "Beta", Description = "hay" };
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { q1, q2 });

        var sut = new GetAllQuestsQueryHandler(questRepo, subjectRepo, logger);
        var res = await sut.Handle(new GetAllQuestsQuery { Page = 1, PageSize = 10, Search = "needle" }, CancellationToken.None);

        res.Items.Should().HaveCount(1);
        res.Items[0].Title.Should().Be("Alpha");
        res.TotalCount.Should().Be(1);
        res.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Handle_FiltersByStatus()
    {
        var questRepo = Substitute.For<IQuestRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<GetAllQuestsQueryHandler>>();

        var q1 = new Quest { Id = Guid.NewGuid(), Title = "A", Status = QuestStatus.InProgress };
        var q2 = new Quest { Id = Guid.NewGuid(), Title = "B", Status = QuestStatus.NotStarted };
        questRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { q1, q2 });

        var sut = new GetAllQuestsQueryHandler(questRepo, subjectRepo, logger);
        var res = await sut.Handle(new GetAllQuestsQuery { Page = 1, PageSize = 10, Status = "InProgress" }, CancellationToken.None);

        res.Items.Should().HaveCount(1);
        res.Items[0].Title.Should().Be("A");
        res.TotalCount.Should().Be(1);
        res.TotalPages.Should().Be(1);
    }
}

