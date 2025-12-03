using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
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
    [Theory]
    [AutoData]
    public async Task Quiz_NoSubmission_ReturnsFalse(Guid activityId, Guid userId)
    {
        var repo = Substitute.For<IQuestSubmissionRepository>();
        var logger = Substitute.For<ILogger<ActivityValidationService>>();
        repo.GetLatestByActivityAndUserAsync(activityId, userId, Arg.Any<CancellationToken>())
            .Returns((QuestSubmission?)null);

        var sut = new ActivityValidationService(repo, logger);
        var (canComplete, message) = await sut.ValidateActivityCompletion(activityId, userId, "Quiz", CancellationToken.None);

        canComplete.Should().BeFalse();
        message.Should().Contain("Quiz must be submitted");
    }

    [Theory]
    [AutoData]
    public async Task Quiz_IsPassedNull_ReturnsFalse(Guid activityId, Guid userId)
    {
        var repo = Substitute.For<IQuestSubmissionRepository>();
        var logger = Substitute.For<ILogger<ActivityValidationService>>();
        var submission = new QuestSubmission { IsPassed = null, Grade = 70, MaxGrade = 100 };
        repo.GetLatestByActivityAndUserAsync(activityId, userId, Arg.Any<CancellationToken>())
            .Returns(submission);

        var sut = new ActivityValidationService(repo, logger);
        var (canComplete, message) = await sut.ValidateActivityCompletion(activityId, userId, "Quiz", CancellationToken.None);

        canComplete.Should().BeFalse();
        message.Should().Contain("invalid");
    }

    [Theory]
    [AutoData]
    public async Task Quiz_Failed_ReturnsFalse(Guid activityId, Guid userId)
    {
        var repo = Substitute.For<IQuestSubmissionRepository>();
        var logger = Substitute.For<ILogger<ActivityValidationService>>();
        var submission = new QuestSubmission { IsPassed = false, Grade = 60, MaxGrade = 100 };
        repo.GetLatestByActivityAndUserAsync(activityId, userId, Arg.Any<CancellationToken>())
            .Returns(submission);

        var sut = new ActivityValidationService(repo, logger);
        var (canComplete, message) = await sut.ValidateActivityCompletion(activityId, userId, "Quiz", CancellationToken.None);

        canComplete.Should().BeFalse();
        message.Should().Contain("not passed");
    }

    [Theory]
    [AutoData]
    public async Task Quiz_Passed_ReturnsTrue(Guid activityId, Guid userId)
    {
        var repo = Substitute.For<IQuestSubmissionRepository>();
        var logger = Substitute.For<ILogger<ActivityValidationService>>();
        var submission = new QuestSubmission { IsPassed = true, Grade = 85, MaxGrade = 100 };
        repo.GetLatestByActivityAndUserAsync(activityId, userId, Arg.Any<CancellationToken>())
            .Returns(submission);

        var sut = new ActivityValidationService(repo, logger);
        var (canComplete, message) = await sut.ValidateActivityCompletion(activityId, userId, "Quiz", CancellationToken.None);

        canComplete.Should().BeTrue();
        message.Should().Contain("passed");
    }

    [Theory]
    [InlineAutoData("KnowledgeCheck")]
    [InlineAutoData("Reading")]
    [InlineAutoData("Coding")]
    public async Task NonQuiz_Types_ReturnTrue(string activityType, Guid activityId, Guid userId)
    {
        var repo = Substitute.For<IQuestSubmissionRepository>();
        var logger = Substitute.For<ILogger<ActivityValidationService>>();
        var sut = new ActivityValidationService(repo, logger);

        var (canComplete, _) = await sut.ValidateActivityCompletion(activityId, userId, activityType, CancellationToken.None);
        canComplete.Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public async Task Unknown_Type_ReturnsTrue(Guid activityId, Guid userId)
    {
        var repo = Substitute.For<IQuestSubmissionRepository>();
        var logger = Substitute.For<ILogger<ActivityValidationService>>();
        var sut = new ActivityValidationService(repo, logger);

        var (canComplete, message) = await sut.ValidateActivityCompletion(activityId, userId, "UnknownType", CancellationToken.None);
        canComplete.Should().BeTrue();
        message.Should().NotBeNullOrEmpty();
    }
}