using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.QuestProgress.Queries.GetCompletedActivities;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;
using Newtonsoft.Json.Linq;

namespace RogueLearn.User.Application.Tests.Features.QuestProgress.Queries.GetCompletedActivities;

public class GetCompletedActivitiesQueryHandlerTests
{
    private static object BuildContent(Guid[] ids)
    {
        var activities = ids.Select(id => new Dictionary<string, object>
        {
            ["activityId"] = id,
            ["type"] = "Reading",
            ["payload"] = new Dictionary<string, object> { ["experiencePoints"] = 5 }
        }).Cast<object>().ToList();
        return new Dictionary<string, object> { ["activities"] = activities };
    }

    [Fact]
    public async Task Handle_NoAttempt_ReturnsEmpty()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var logger = Substitute.For<ILogger<GetCompletedActivitiesQueryHandler>>();
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, logger);

        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var query = new GetCompletedActivitiesQuery { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid(), StepId = Guid.NewGuid() };
        var questStep = new QuestStep { Id = query.StepId, QuestId = query.QuestId, Content = BuildContent(ids) };
        questStepRepo.GetByIdAsync(query.StepId, Arg.Any<CancellationToken>()).Returns(questStep);
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);

        var result = await sut.Handle(query, CancellationToken.None);
        result.TotalCount.Should().Be(3);
        result.CompletedCount.Should().Be(0);
        result.Activities.All(a => !a.IsCompleted).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithProgress_MarksCompleted()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var logger = Substitute.For<ILogger<GetCompletedActivitiesQueryHandler>>();
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, logger);

        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var query = new GetCompletedActivitiesQuery { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid(), StepId = Guid.NewGuid() };
        var questStep = new QuestStep { Id = query.StepId, QuestId = query.QuestId, Content = BuildContent(ids) };
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = query.AuthUserId, QuestId = query.QuestId };
        var progress = new UserQuestStepProgress { AttemptId = attempt.Id, StepId = query.StepId, CompletedActivityIds = new[] { ids[1], ids[3] } };

        questStepRepo.GetByIdAsync(query.StepId, Arg.Any<CancellationToken>()).Returns(questStep);
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        var result = await sut.Handle(query, CancellationToken.None);
        result.TotalCount.Should().Be(4);
        result.CompletedCount.Should().Be(2);
        result.Activities.Count(a => a.IsCompleted).Should().Be(2);
    }

    [Fact]
    public async Task Handle_WithStringActivityIds_ParsesAndMarksCompleted()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var logger = Substitute.For<ILogger<GetCompletedActivitiesQueryHandler>>();
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, logger);

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var query = new GetCompletedActivitiesQuery { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid(), StepId = Guid.NewGuid() };
        var activities = new List<object>
        {
            new Dictionary<string, object> { ["activityId"] = id1.ToString(), ["type"] = "Reading", ["payload"] = new Dictionary<string, object> { ["experiencePoints"] = 5 } },
            new Dictionary<string, object> { ["activityId"] = id2.ToString(), ["type"] = "Quiz" }
        };
        var questStep = new QuestStep { Id = query.StepId, QuestId = query.QuestId, Content = new Dictionary<string, object> { ["activities"] = activities } };

        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = query.AuthUserId, QuestId = query.QuestId };
        var progress = new UserQuestStepProgress { AttemptId = attempt.Id, StepId = query.StepId, CompletedActivityIds = new[] { id2 } };

        questStepRepo.GetByIdAsync(query.StepId, Arg.Any<CancellationToken>()).Returns(questStep);
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        var result = await sut.Handle(query, CancellationToken.None);
        result.TotalCount.Should().Be(2);
        result.CompletedCount.Should().Be(1);
        result.Activities.Count(a => a.IsCompleted).Should().Be(1);
    }

    [Fact]
    public async Task Handle_StepBelongsToDifferentQuest_Throws()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var logger = Substitute.For<ILogger<GetCompletedActivitiesQueryHandler>>();
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, logger);

        var query = new GetCompletedActivitiesQuery { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid(), StepId = Guid.NewGuid() };
        var questStep = new QuestStep { Id = query.StepId, QuestId = Guid.NewGuid(), Content = BuildContent(new[] { Guid.NewGuid() }) };
        questStepRepo.GetByIdAsync(query.StepId, Arg.Any<CancellationToken>()).Returns(questStep);

        var act = () => sut.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_StepNotFound_Throws()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var logger = Substitute.For<ILogger<GetCompletedActivitiesQueryHandler>>();
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, logger);

        var query = new GetCompletedActivitiesQuery { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid(), StepId = Guid.NewGuid() };
        questStepRepo.GetByIdAsync(query.StepId, Arg.Any<CancellationToken>()).Returns((QuestStep?)null);

        var act = () => sut.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_JObjectContent_ParsesActivities()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var logger = Substitute.For<ILogger<GetCompletedActivitiesQueryHandler>>();
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, logger);

        var id = Guid.NewGuid();
        var activities = new Newtonsoft.Json.Linq.JArray
        {
            new Newtonsoft.Json.Linq.JObject
            {
                ["activityId"] = id.ToString(),
                ["type"] = "Reading",
                ["payload"] = new Newtonsoft.Json.Linq.JObject { ["experiencePoints"] = 5 }
            }
        };
        var j = new Newtonsoft.Json.Linq.JObject { ["activities"] = activities };

        var query = new GetCompletedActivitiesQuery { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid(), StepId = Guid.NewGuid() };
        var questStep = new QuestStep { Id = query.StepId, QuestId = query.QuestId, Content = j };
        questStepRepo.GetByIdAsync(query.StepId, Arg.Any<CancellationToken>()).Returns(questStep);
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = query.AuthUserId, QuestId = query.QuestId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { AttemptId = attempt.Id, StepId = query.StepId, CompletedActivityIds = new[] { id } };
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        var result = await sut.Handle(query, CancellationToken.None);
        result.TotalCount.Should().Be(1);
        result.CompletedCount.Should().Be(1);
    }

    private class JObject
    {
        public override string ToString() => "{ invalid";
    }

    [Fact]
    public async Task Handle_JObjectContent_InvalidJson_Caught_ReturnsEmpty()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var logger = Substitute.For<ILogger<GetCompletedActivitiesQueryHandler>>();
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, logger);

        var query = new GetCompletedActivitiesQuery { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid(), StepId = Guid.NewGuid() };
        var questStep = new QuestStep { Id = query.StepId, QuestId = query.QuestId, Content = new JObject() };
        questStepRepo.GetByIdAsync(query.StepId, Arg.Any<CancellationToken>()).Returns(questStep);
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);

        var result = await sut.Handle(query, CancellationToken.None);
        result.TotalCount.Should().Be(0);
        result.CompletedCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ParseActivityElement_MapsFields()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var logger = Substitute.For<ILogger<GetCompletedActivitiesQueryHandler>>();
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, logger);

        var id = Guid.NewGuid();
        var payload = new Newtonsoft.Json.Linq.JObject
        {
            ["experiencePoints"] = 7,
            ["skillId"] = Guid.NewGuid().ToString(),
            ["articleTitle"] = "A"
        };
        var activities = new Newtonsoft.Json.Linq.JArray
        {
            new Newtonsoft.Json.Linq.JObject
            {
                ["activityId"] = id.ToString(),
                ["type"] = "Reading",
                ["payload"] = payload
            }
        };
        var content = new Newtonsoft.Json.Linq.JObject { ["activities"] = activities };
        var query = new GetCompletedActivitiesQuery { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid(), StepId = Guid.NewGuid() };
        var questStep = new QuestStep { Id = query.StepId, QuestId = query.QuestId, Content = content };
        questStepRepo.GetByIdAsync(query.StepId, Arg.Any<CancellationToken>()).Returns(questStep);
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);

        var result = await sut.Handle(query, CancellationToken.None);
        result.TotalCount.Should().Be(1);
        result.Activities[0].ActivityType.Should().Be("Reading");
        result.Activities[0].Title.Should().Be("A");
        result.Activities[0].ExperiencePoints.Should().Be(7);
    }

    [Fact]
    public async Task Handle_ParseActivityDict_MapsFields()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var logger = Substitute.For<ILogger<GetCompletedActivitiesQueryHandler>>();
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, logger);

        var id = Guid.NewGuid();
        var activities = new List<object>
        {
            new Dictionary<string, object>
            {
                ["activityId"] = id,
                ["type"] = "KnowledgeCheck",
                ["payload"] = new Dictionary<string, object> { ["experiencePoints"] = 10, ["topic"] = "T" }
            }
        };
        var content = new Dictionary<string, object> { ["activities"] = activities };
        var query = new GetCompletedActivitiesQuery { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid(), StepId = Guid.NewGuid() };
        var questStep = new QuestStep { Id = query.StepId, QuestId = query.QuestId, Content = content };
        questStepRepo.GetByIdAsync(query.StepId, Arg.Any<CancellationToken>()).Returns(questStep);
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);

        var result = await sut.Handle(query, CancellationToken.None);
        result.TotalCount.Should().Be(1);
        result.Activities[0].ActivityType.Should().Be("KnowledgeCheck");
        result.Activities[0].Title.Should().Be("T");
        result.Activities[0].ExperiencePoints.Should().Be(10);
    }
}
