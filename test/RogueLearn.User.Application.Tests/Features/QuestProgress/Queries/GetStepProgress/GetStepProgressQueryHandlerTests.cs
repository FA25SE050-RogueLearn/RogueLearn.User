using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.QuestProgress.Queries.GetStepProgress;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.QuestProgress.Queries.GetStepProgress;

public class GetStepProgressQueryHandlerTests
{
    private static object BuildContentWithActivities(int count)
    {
        var activities = new List<object>();
        for (int i = 0; i < count; i++)
        {
            activities.Add(new Dictionary<string, object>
            {
                ["activityId"] = Guid.NewGuid(),
                ["type"] = "Reading",
                ["payload"] = new Dictionary<string, object> { ["experiencePoints"] = 10 }
            });
        }
        return new Dictionary<string, object> { ["activities"] = activities };
    }

    [Theory]
    [AutoData]
    public async Task Handle_NoStepProgress_ReturnsEmpty(GetStepProgressQuery query)
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var logger = Substitute.For<ILogger<GetStepProgressQueryHandler>>();
        var sut = new GetStepProgressQueryHandler(attemptRepo, stepRepo, questStepRepo, logger);

        var questStep = new QuestStep { Id = query.StepId, QuestId = query.QuestId, Title = "Step", Content = BuildContentWithActivities(3) };
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = query.AuthUserId, QuestId = query.QuestId };
        questStepRepo.GetByIdAsync(query.StepId, Arg.Any<CancellationToken>()).Returns(questStep);
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestStepProgress?)null);

        var result = await sut.Handle(query, CancellationToken.None);
        result!.TotalActivitiesCount.Should().Be(3);
        result.CompletedActivitiesCount.Should().Be(0);
        result.ProgressPercentage.Should().Be(0);
    }

    [Theory]
    [AutoData]
    public async Task Handle_WithProgress_ComputesCounts(GetStepProgressQuery query)
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var logger = Substitute.For<ILogger<GetStepProgressQueryHandler>>();
        var sut = new GetStepProgressQueryHandler(attemptRepo, stepRepo, questStepRepo, logger);

        var content = BuildContentWithActivities(4);
        var questStep = new QuestStep { Id = query.StepId, QuestId = query.QuestId, Title = "Step", Content = content };
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = query.AuthUserId, QuestId = query.QuestId };
        var progress = new UserQuestStepProgress { AttemptId = attempt.Id, StepId = query.StepId, CompletedActivityIds = new[] { Guid.NewGuid(), Guid.NewGuid() }, Status = RogueLearn.User.Domain.Enums.StepCompletionStatus.InProgress };

        questStepRepo.GetByIdAsync(query.StepId, Arg.Any<CancellationToken>()).Returns(questStep);
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        var result = await sut.Handle(query, CancellationToken.None);
        result!.TotalActivitiesCount.Should().Be(4);
        result.CompletedActivitiesCount.Should().Be(2);
        result.ProgressPercentage.Should().Be(50);
    }
}