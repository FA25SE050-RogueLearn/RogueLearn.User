using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Quests.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Quests.Services;

public class ActivityValidationServiceTests
{
    [Fact]
    public async Task Quiz_NoSubmission_ReturnsFalse()
    {
        var repo = Substitute.For<IQuestSubmissionRepository>();
        var logger = Substitute.For<ILogger<ActivityValidationService>>();
        var activityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        repo.GetLatestByActivityAndUserAsync(activityId, userId, Arg.Any<CancellationToken>())
            .Returns((QuestSubmission?)null);

        var sut = new ActivityValidationService(repo, logger);
        var (canComplete, message) = await sut.ValidateActivityCompletion(activityId, userId, "Quiz", CancellationToken.None);

        canComplete.Should().BeFalse();
        message.Should().Contain("Quiz must be submitted");
    }

    [Fact]
    public async Task Quiz_IsPassedNull_ReturnsFalse()
    {
        var repo = Substitute.For<IQuestSubmissionRepository>();
        var logger = Substitute.For<ILogger<ActivityValidationService>>();
        var submission = new QuestSubmission { IsPassed = null, Grade = 70, MaxGrade = 100 };
        var activityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        repo.GetLatestByActivityAndUserAsync(activityId, userId, Arg.Any<CancellationToken>())
            .Returns(submission);

        var sut = new ActivityValidationService(repo, logger);
        var (canComplete, message) = await sut.ValidateActivityCompletion(activityId, userId, "Quiz", CancellationToken.None);

        canComplete.Should().BeFalse();
        message.Should().Contain("invalid");
    }

    [Fact]
    public async Task Quiz_Failed_ReturnsFalse()
    {
        var repo = Substitute.For<IQuestSubmissionRepository>();
        var logger = Substitute.For<ILogger<ActivityValidationService>>();
        var submission = new QuestSubmission { IsPassed = false, Grade = 60, MaxGrade = 100 };
        var activityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        repo.GetLatestByActivityAndUserAsync(activityId, userId, Arg.Any<CancellationToken>())
            .Returns(submission);

        var sut = new ActivityValidationService(repo, logger);
        var (canComplete, message) = await sut.ValidateActivityCompletion(activityId, userId, "Quiz", CancellationToken.None);

        canComplete.Should().BeFalse();
        message.Should().Contain("not passed");
    }

    [Fact]
    public async Task Quiz_Passed_ReturnsTrue()
    {
        var repo = Substitute.For<IQuestSubmissionRepository>();
        var logger = Substitute.For<ILogger<ActivityValidationService>>();
        var submission = new QuestSubmission { IsPassed = true, Grade = 85, MaxGrade = 100 };
        var activityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        repo.GetLatestByActivityAndUserAsync(activityId, userId, Arg.Any<CancellationToken>())
            .Returns(submission);

        var sut = new ActivityValidationService(repo, logger);
        var (canComplete, message) = await sut.ValidateActivityCompletion(activityId, userId, "Quiz", CancellationToken.None);

        canComplete.Should().BeTrue();
        message.Should().Contain("passed");
    }

    [Fact]
    public async Task NonQuiz_Types_ReturnTrue()
    {
        var repo = Substitute.For<IQuestSubmissionRepository>();
        var logger = Substitute.For<ILogger<ActivityValidationService>>();
        var sut = new ActivityValidationService(repo, logger);
        var activityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        foreach (var activityType in new[] { "KnowledgeCheck", "Reading", "Coding" })
        {
            var (canComplete, _) = await sut.ValidateActivityCompletion(activityId, userId, activityType, CancellationToken.None);
            canComplete.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Unknown_Type_ReturnsTrue()
    {
        var repo = Substitute.For<IQuestSubmissionRepository>();
        var logger = Substitute.For<ILogger<ActivityValidationService>>();
        var sut = new ActivityValidationService(repo, logger);

        var activityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (canComplete, message) = await sut.ValidateActivityCompletion(activityId, userId, "UnknownType", CancellationToken.None);
        canComplete.Should().BeTrue();
        message.Should().NotBeNullOrEmpty();
    }
}