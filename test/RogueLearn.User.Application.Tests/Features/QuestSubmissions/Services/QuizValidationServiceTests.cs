using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.QuestSubmissions.Services;

namespace RogueLearn.User.Application.Tests.Features.QuestSubmissions.Services;

public class QuizValidationServiceTests
{
    private QuizValidationService CreateService()
    {
        var logger = Substitute.For<ILogger<QuizValidationService>>();
        return new QuizValidationService(logger);
    }

    [Fact]
    public void ValidateQuizScore_ShouldPass_WhenScoreMeetsThreshold()
    {
        var service = CreateService();
        var result = service.ValidateQuizScore(7, 10);
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateQuizScore_ShouldFail_WhenScoreBelowThreshold()
    {
        var service = CreateService();
        var result = service.ValidateQuizScore(6, 10);
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateQuizScore_ShouldFail_WhenTotalQuestionsZero()
    {
        var service = CreateService();
        var result = service.ValidateQuizScore(0, 0);
        result.Should().BeFalse();
    }

    [Fact]
    public void CalculateQuizPercentage_ShouldReturnRoundedPercentage()
    {
        var service = CreateService();
        service.CalculateQuizPercentage(7, 10).Should().Be(70.00m);
        service.CalculateQuizPercentage(2, 3).Should().Be(66.67m);
        service.CalculateQuizPercentage(0, 0).Should().Be(0.00m);
    }

    [Fact]
    public void EvaluateQuizSubmission_ShouldReturnTupleWithPassAndPercentage()
    {
        var service = CreateService();
        var (isPassed, percentage) = service.EvaluateQuizSubmission(7, 10);
        isPassed.Should().BeTrue();
        percentage.Should().Be(70.00m);
    }
}