using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.QuestSubmissions.Services;

namespace RogueLearn.User.Application.Tests.Features.QuestSubmissions.Services;

public class KnowledgeCheckValidationServiceTests
{
    private KnowledgeCheckValidationService CreateService()
    {
        var logger = Substitute.For<ILogger<KnowledgeCheckValidationService>>();
        return new KnowledgeCheckValidationService(logger);
    }

    [Fact]
    public void ValidateKnowledgeCheckScore_ShouldPass_WhenAllAnswersCorrect()
    {
        var service = CreateService();
        var result = service.ValidateKnowledgeCheckScore(5, 5);
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateKnowledgeCheckScore_ShouldFail_WhenAnyAnswerIncorrect()
    {
        var service = CreateService();
        var result = service.ValidateKnowledgeCheckScore(4, 5);
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateKnowledgeCheckScore_ShouldFail_WhenTotalQuestionsZero()
    {
        var service = CreateService();
        var result = service.ValidateKnowledgeCheckScore(0, 0);
        result.Should().BeFalse();
    }

    [Fact]
    public void CalculateKnowledgeCheckPercentage_ShouldReturnRoundedPercentage()
    {
        var service = CreateService();
        service.CalculateKnowledgeCheckPercentage(5, 5).Should().Be(100.00m);
        service.CalculateKnowledgeCheckPercentage(4, 5).Should().Be(80.00m);
        service.CalculateKnowledgeCheckPercentage(0, 0).Should().Be(0.00m);
    }

    [Fact]
    public void EvaluateKnowledgeCheckSubmission_ShouldReturnTupleWithPassAndPercentage()
    {
        var service = CreateService();
        var (isPassed, percentage) = service.EvaluateKnowledgeCheckSubmission(5, 5);
        isPassed.Should().BeTrue();
        percentage.Should().Be(100.00m);
    }
}