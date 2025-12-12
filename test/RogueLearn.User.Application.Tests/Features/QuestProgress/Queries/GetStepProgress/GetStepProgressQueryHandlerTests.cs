using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.QuestProgress.Queries.GetStepProgress;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.QuestProgress.Queries.GetStepProgress;

public class GetStepProgressQueryHandlerTests
{
    [Fact]
    public async Task CompletedStatus_OverridesPercentageTo100()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var questStep = new QuestStep { Id = stepId, QuestId = questId, Title = "T", Content = new[] { new { a = 1 }, new { a = 2 }, new { a = 3 } } };
        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(questStep);
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.Completed, CompletedActivityIds = new[] { Guid.NewGuid() } };
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);
        var sut = new GetStepProgressQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetStepProgressQueryHandler>>());
        var res = await sut.Handle(new GetStepProgressQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None);
        res!.ProgressPercentage.Should().Be(100);
        res.CompletedActivitiesCount.Should().Be(1);
        res.TotalActivitiesCount.Should().Be(3);
    }

    [Fact]
    public async Task NoStepProgress_ReturnsZeroProgress()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var questStep = new QuestStep { Id = stepId, QuestId = questId, Title = "T", Content = new[] { new { x = 1 } } };
        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(questStep);
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestStepProgress?)null);
        var sut = new GetStepProgressQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetStepProgressQueryHandler>>());
        var res = await sut.Handle(new GetStepProgressQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None);
        res!.CompletedActivitiesCount.Should().Be(0);
        res.ProgressPercentage.Should().Be(0);
    }

    [Fact]
    public async Task StepNotFoundOrWrongQuest_ThrowsNotFound()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns((QuestStep?)null);
        var sut = new GetStepProgressQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetStepProgressQueryHandler>>());
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.NotFoundException>(() => sut.Handle(new GetStepProgressQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None));
    }

    [Fact]
    public async Task AttemptNotFound_ThrowsNotFound()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var questStep = new QuestStep { Id = stepId, QuestId = questId, Title = "T", Content = new { activities = new[] { new { a = 1 } } } };
        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(questStep);
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestAttempt?)null);
        var sut = new GetStepProgressQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetStepProgressQueryHandler>>());
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.NotFoundException>(() => sut.Handle(new GetStepProgressQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None));
    }

    [Fact]
    public async Task NoProgress_ReturnsDefaultWithExtractedActivityCount()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var questStep = new QuestStep { Id = stepId, QuestId = questId, Title = "T", Content = new { activities = new[] { new { a = 1 }, new { a = 2 }, new { a = 3 }, new { a = 4 } } } };
        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(questStep);
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestStepProgress?)null);

        var sut = new GetStepProgressQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetStepProgressQueryHandler>>());
        var res = await sut.Handle(new GetStepProgressQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None);
        res!.TotalActivitiesCount.Should().Be(4);
        res.CompletedActivitiesCount.Should().Be(0);
        res.ProgressPercentage.Should().Be(0);
        res.Status.Should().Be("InProgress");
    }

    [Fact]
    public async Task PartialProgress_ComputesPercentageFromCounts()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var contentString = "{\"activities\":[{},{},{},{}]}";
        var questStep = new QuestStep { Id = stepId, QuestId = questId, Title = "T", Content = contentString };
        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(questStep);
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        var progress = new UserQuestStepProgress { AttemptId = attempt.Id, StepId = stepId, Status = StepCompletionStatus.InProgress, CompletedActivityIds = new[] { Guid.NewGuid() } };
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns(progress);

        var sut = new GetStepProgressQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetStepProgressQueryHandler>>());
        var res = await sut.Handle(new GetStepProgressQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None);
        res!.TotalActivitiesCount.Should().Be(4);
        res.CompletedActivitiesCount.Should().Be(1);
        res.ProgressPercentage.Should().Be(25.00m);
        res.Status.Should().Be("InProgress");
        }

        [Fact]
        public async Task ActivitiesPropertyExistsButNotArray_ReturnsZero()
        {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var contentString = "{\"activities\":{}}";
        var questStep = new QuestStep { Id = stepId, QuestId = questId, Title = "T", Content = contentString };
        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(questStep);
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestStepProgress?)null);

        var sut = new GetStepProgressQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetStepProgressQueryHandler>>());
        var res = await sut.Handle(new GetStepProgressQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None);
        res!.TotalActivitiesCount.Should().Be(0);
        res.CompletedActivitiesCount.Should().Be(0);
        }

        [Fact]
        public async Task ExtractActivityCount_InvalidJson_LogsErrorAndReturnsZero()
        {
            var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
            var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
            var questStepRepo = Substitute.For<IQuestStepRepository>();
            var stepId = Guid.NewGuid();
            var questId = Guid.NewGuid();
            var authId = Guid.NewGuid();
            var questStep = new QuestStep { Id = stepId, QuestId = questId, Title = "T", Content = "not json" };
            questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(questStep);

            var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
            attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
            stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestStepProgress?)null);

            var sut = new GetStepProgressQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetStepProgressQueryHandler>>());
            var res = await sut.Handle(new GetStepProgressQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None);
            res!.TotalActivitiesCount.Should().Be(0);
            res.CompletedActivitiesCount.Should().Be(0);
        }

    [Fact]
    public async Task ContentJArray_CountsActivities()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var jarr = Newtonsoft.Json.Linq.JArray.Parse("[{},{}]");
        var questStep = new QuestStep { Id = stepId, QuestId = questId, Title = "T", Content = jarr };
        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(questStep);
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestStepProgress?)null);

        var sut = new GetStepProgressQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetStepProgressQueryHandler>>());
        var res = await sut.Handle(new GetStepProgressQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None);
        res!.TotalActivitiesCount.Should().Be(2);
        res.CompletedActivitiesCount.Should().Be(0);
    }

    [Fact]
    public async Task NullContent_ReturnsZeroTotals()
    {
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var stepId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        var questStep = new QuestStep { Id = stepId, QuestId = questId, Title = "T", Content = null };
        questStepRepo.GetByIdAsync(stepId, Arg.Any<CancellationToken>()).Returns(questStep);
        var attempt = new UserQuestAttempt { Id = Guid.NewGuid(), AuthUserId = authId, QuestId = questId };
        attemptRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(attempt);
        stepRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestStepProgress, bool>>>(), Arg.Any<CancellationToken>()).Returns((UserQuestStepProgress?)null);

        var sut = new GetStepProgressQueryHandler(attemptRepo, stepRepo, questStepRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<GetStepProgressQueryHandler>>());
        var res = await sut.Handle(new GetStepProgressQuery { AuthUserId = authId, QuestId = questId, StepId = stepId }, CancellationToken.None);
        res!.TotalActivitiesCount.Should().Be(0);
        res.CompletedActivitiesCount.Should().Be(0);
        res.ProgressPercentage.Should().Be(0);
    }
}
