using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestStepContent;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Quests.Queries.GetQuestStepContent;

public class GetQuestStepContentQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsActivitiesFromContent()
    {
        var repo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var content = new Dictionary<string, object> { ["activities"] = new List<object> { new Dictionary<string, object> { ["activityId"] = Guid.NewGuid(), ["type"] = "t", ["payload"] = new Dictionary<string, object> { ["x"] = 1 } } } };
        repo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, Content = content });

        var sut = new GetQuestStepContentQueryHandler(repo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetQuestStepContentQueryHandler>>());
        var res = await sut.Handle(new GetQuestStepContentQuery { QuestStepId = stepId }, CancellationToken.None);
        res.Activities.Should().HaveCount(1);
    }
}