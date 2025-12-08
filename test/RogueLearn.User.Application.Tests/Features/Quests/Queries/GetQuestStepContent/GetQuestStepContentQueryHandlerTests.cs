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

    [Fact]
    public async Task Handle_StepMissing_ThrowsNotFound()
    {
        var repo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        repo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns((QuestStep?)null);

        var sut = new GetQuestStepContentQueryHandler(repo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetQuestStepContentQueryHandler>>());
        var act = () => sut.Handle(new GetQuestStepContentQuery { QuestStepId = stepId }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task Handle_NullContent_ReturnsEmptyResponse()
    {
        var repo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        repo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, Content = null });

        var sut = new GetQuestStepContentQueryHandler(repo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetQuestStepContentQueryHandler>>());
        var res = await sut.Handle(new GetQuestStepContentQuery { QuestStepId = stepId }, CancellationToken.None);
        res.Activities.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MalformedJsonString_ThrowsInvalidOperation()
    {
        var repo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        repo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, Content = "{ invalid json" });

        var sut = new GetQuestStepContentQueryHandler(repo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetQuestStepContentQueryHandler>>());
        var act = () => sut.Handle(new GetQuestStepContentQuery { QuestStepId = stepId }, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_JTokenContent_ParsesActivities()
    {
        var repo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();

        var jObj = Newtonsoft.Json.Linq.JObject.Parse("{\"activities\":[{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"t\",\"payload\":{\"x\":1}}]}");
        repo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, Content = jObj });

        var sut = new GetQuestStepContentQueryHandler(repo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetQuestStepContentQueryHandler>>());
        var res = await sut.Handle(new GetQuestStepContentQuery { QuestStepId = stepId }, CancellationToken.None);
        res.Activities.Should().HaveCount(1);
        res.Activities[0].Type.Should().Be("t");
        var payload = res.Activities[0].Payload as Dictionary<string, object>;
        payload.Should().NotBeNull();
        payload!.ContainsKey("x").Should().BeTrue();
    }

    [Fact]
    public async Task Handle_StringJsonContent_ParsesActivities()
    {
        var repo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var json = "{\"activities\":[{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"x\",\"payload\":{\"y\":2}}]}";
        repo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, Content = json });

        var sut = new GetQuestStepContentQueryHandler(repo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetQuestStepContentQueryHandler>>());
        var res = await sut.Handle(new GetQuestStepContentQuery { QuestStepId = stepId }, CancellationToken.None);
        res.Activities.Should().HaveCount(1);
        res.Activities[0].Type.Should().Be("x");
    }

    [Fact]
    public async Task Handle_NestedPayloadTypes_Converted()
    {
        var repo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var json = "{\"activities\":[{\"activityId\":\"" + Guid.NewGuid() + "\",\"type\":\"t\",\"payload\":{\"arr\":[1,\"x\",{\"y\":true}],\"num\":3.14}}]}";
        repo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, Content = json });

        var sut = new GetQuestStepContentQueryHandler(repo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetQuestStepContentQueryHandler>>());
        var res = await sut.Handle(new GetQuestStepContentQuery { QuestStepId = stepId }, CancellationToken.None);
        res.Activities.Should().HaveCount(1);
        var payload = res.Activities[0].Payload as Dictionary<string, object>;
        var arr = payload!["arr"] as List<object>;
        arr![0].Should().BeOfType<long>();
        (arr![2] as Dictionary<string, object>)!["y"].Should().BeOfType<bool>();
        payload!["num"].Should().BeOfType<double>();
    }

    [Fact]
    public async Task Handle_DateToken_ConvertsToString()
    {
        var repo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var j = new Newtonsoft.Json.Linq.JObject
        {
            ["activities"] = new Newtonsoft.Json.Linq.JArray
            {
                new Newtonsoft.Json.Linq.JObject
                {
                    ["activityId"] = Guid.NewGuid().ToString(),
                    ["type"] = "t",
                    ["payload"] = new Newtonsoft.Json.Linq.JObject
                    {
                        ["when"] = new Newtonsoft.Json.Linq.JValue(new DateTime(2024,1,1))
                    }
                }
            }
        };
        repo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(new QuestStep { Id = stepId, Content = j });

        var sut = new GetQuestStepContentQueryHandler(repo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetQuestStepContentQueryHandler>>());
        var res = await sut.Handle(new GetQuestStepContentQuery { QuestStepId = stepId }, CancellationToken.None);
        var payload = res.Activities[0].Payload as Dictionary<string, object>;
        payload!["when"].Should().BeOfType<string>();
    }
    [Fact]
    public async Task Handle_RepositoryThrows_Rethrows()
    {
        var repo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        repo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns<Task<QuestStep?>>(_ => throw new Exception("db"));

        var sut = new GetQuestStepContentQueryHandler(repo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetQuestStepContentQueryHandler>>());
        await Assert.ThrowsAsync<Exception>(() => sut.Handle(new GetQuestStepContentQuery { QuestStepId = stepId }, CancellationToken.None));
    }
}
