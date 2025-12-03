using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.QuestProgress.Queries.GetCompletedActivities;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

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

    [Theory]
    [AutoData]
    public async Task Handle_NoAttempt_ReturnsEmpty(GetCompletedActivitiesQuery query)
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var logger = Substitute.For<ILogger<GetCompletedActivitiesQueryHandler>>();
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, logger);

        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var questStep = new QuestStep { Id = query.StepId, QuestId = query.QuestId, Content = BuildContent(ids) };
        questStepRepo.GetByIdAsync(query.StepId, Arg.Any<CancellationToken>()).Returns(questStep);
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);

        var result = await sut.Handle(query, CancellationToken.None);
        result.TotalCount.Should().Be(3);
        result.CompletedCount.Should().Be(0);
        result.Activities.All(a => !a.IsCompleted).Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public async Task Handle_WithProgress_MarksCompleted(GetCompletedActivitiesQuery query)
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var logger = Substitute.For<ILogger<GetCompletedActivitiesQueryHandler>>();
        var sut = new GetCompletedActivitiesQueryHandler(attemptRepo, stepRepo, questStepRepo, logger);

        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
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
}