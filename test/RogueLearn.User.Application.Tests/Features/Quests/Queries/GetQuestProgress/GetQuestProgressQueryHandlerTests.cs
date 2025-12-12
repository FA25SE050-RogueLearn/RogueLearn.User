using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Queries.GetQuestProgress;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Quests.Queries.GetQuestProgress;

public class GetQuestProgressQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoAttempt_ThrowsNotFound()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();

        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);

        var sut = new GetQuestProgressQueryHandler(attemptRepo, stepRepo, progressRepo);
        var act = () => sut.Handle(new GetQuestProgressQuery { AuthUserId = Guid.NewGuid(), QuestId = Guid.NewGuid() }, CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task Handle_FiltersByAssignedDifficulty_ComputesLockingAndCounts()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();

        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId, AssignedDifficulty = "Supportive" };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);

        var st1 = new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 1, Title = "S1", DifficultyVariant = "Supportive", Content = new Dictionary<string, object> { ["activities"] = new List<object> { new Dictionary<string, object> { ["activityId"] = Guid.NewGuid().ToString() } } } };
        var st2 = new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 2, Title = "S2", DifficultyVariant = "Supportive", Content = new Dictionary<string, object> { ["activities"] = new List<object> { new Dictionary<string, object> { ["activityId"] = Guid.NewGuid().ToString() }, new Dictionary<string, object> { ["activityId"] = Guid.NewGuid().ToString() } } } };
        var st3 = new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 3, Title = "S3", DifficultyVariant = "Standard" };
        stepRepo.GetByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new[] { st3, st2, st1 });

        var p1 = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = st1.Id, Status = StepCompletionStatus.Completed, CompletedActivityIds = new[] { Guid.NewGuid() } };
        var p2 = new UserQuestStepProgress { Id = Guid.NewGuid(), AttemptId = attempt.Id, StepId = st2.Id, Status = StepCompletionStatus.InProgress, CompletedActivityIds = new[] { Guid.NewGuid() } };
        progressRepo.GetByAttemptIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(new[] { p1, p2 });

        var sut = new GetQuestProgressQueryHandler(attemptRepo, stepRepo, progressRepo);
        var res = await sut.Handle(new GetQuestProgressQuery { AuthUserId = authId, QuestId = questId }, CancellationToken.None);

        res.Should().HaveCount(2);
        res[0].Title.Should().Be("S1");
        res[0].IsLocked.Should().BeFalse();
        res[0].CompletedActivitiesCount.Should().Be(1);
        res[0].TotalActivitiesCount.Should().Be(1);
        res[1].Title.Should().Be("S2");
        res[1].IsLocked.Should().BeFalse();
        res[1].CompletedActivitiesCount.Should().Be(1);
        res[1].TotalActivitiesCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ElseBranch_NoProgressSetsNotStartedAndLockRulesApply()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();

        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId, AssignedDifficulty = "Supportive" };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);

        var st1 = new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 1, Title = "S1", DifficultyVariant = "Supportive", Content = new Dictionary<string, object> { ["activities"] = new List<object>() } };
        var st2 = new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 2, Title = "S2", DifficultyVariant = "Supportive", Content = new Dictionary<string, object> { ["activities"] = new List<object>() } };
        stepRepo.GetByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new[] { st1, st2 });
        progressRepo.GetByAttemptIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserQuestStepProgress>());

        var sut = new GetQuestProgressQueryHandler(attemptRepo, stepRepo, progressRepo);
        var res = await sut.Handle(new GetQuestProgressQuery { AuthUserId = authId, QuestId = questId }, CancellationToken.None);
        res.Should().HaveCount(2);
        res[0].Status.Should().Be(StepCompletionStatus.NotStarted);
        res[1].Status.Should().Be(StepCompletionStatus.NotStarted);
        res[1].IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task CountTotalActivities_CatchesParseError_ReturnsZero()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();

        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId, AssignedDifficulty = "Supportive" };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);

        var step = new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 1, Title = "S", DifficultyVariant = "Supportive", Content = new ThrowingContent() };
        stepRepo.GetByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new[] { step });
        progressRepo.GetByAttemptIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserQuestStepProgress>());

        var sut = new GetQuestProgressQueryHandler(attemptRepo, stepRepo, progressRepo);
        var res = await sut.Handle(new GetQuestProgressQuery { AuthUserId = authId, QuestId = questId }, CancellationToken.None);
        res[0].TotalActivitiesCount.Should().Be(0);
    }

    private class ThrowingContent
    {
        public int Boom => throw new Exception("boom");
    }

    [Fact]
    public async Task Handle_NoStepsForTrack_ReturnsEmpty()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();

        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId, AssignedDifficulty = "Adaptive" };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.GetByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new[] { new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 1, DifficultyVariant = "Standard" } });
        progressRepo.GetByAttemptIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserQuestStepProgress>());

        var sut = new GetQuestProgressQueryHandler(attemptRepo, stepRepo, progressRepo);
        var res = await sut.Handle(new GetQuestProgressQuery { AuthUserId = authId, QuestId = questId }, CancellationToken.None);
        res.Should().BeEmpty();
    }

    [Fact]
    public async Task CountTotalActivities_ActivitiesNotArray_ReturnsZero()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IQuestStepRepository>();
        var progressRepo = Substitute.For<IUserQuestStepProgressRepository>();

        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId, AssignedDifficulty = "Supportive" };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);

        var content = new Dictionary<string, object> { ["activities"] = new Dictionary<string, object> { ["foo"] = 1 } };
        var step = new QuestStep { Id = Guid.NewGuid(), QuestId = questId, StepNumber = 1, Title = "S", DifficultyVariant = "Supportive", Content = content };
        stepRepo.GetByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new[] { step });
        progressRepo.GetByAttemptIdAsync(attempt.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserQuestStepProgress>());

        var sut = new GetQuestProgressQueryHandler(attemptRepo, stepRepo, progressRepo);
        var res = await sut.Handle(new GetQuestProgressQuery { AuthUserId = authId, QuestId = questId }, CancellationToken.None);
        res[0].TotalActivitiesCount.Should().Be(0);
    }
}
