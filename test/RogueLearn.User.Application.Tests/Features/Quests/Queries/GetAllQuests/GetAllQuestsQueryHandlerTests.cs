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
}

