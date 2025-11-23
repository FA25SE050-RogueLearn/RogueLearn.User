// RogueLearn.User/src/RogueLearn.User.Application/Features/QuestSubmissions/Services/KnowledgeCheckValidationService.cs
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.QuestSubmissions.Services;

public class KnowledgeCheckValidationService : IKnowledgeCheckValidationService
{
    private const decimal KNOWLEDGE_CHECK_PASSING_THRESHOLD = 1.00m; // 100%
    private readonly ILogger<KnowledgeCheckValidationService> _logger;

    public KnowledgeCheckValidationService(ILogger<KnowledgeCheckValidationService> logger)
    {
        _logger = logger;
    }

    public bool ValidateKnowledgeCheckScore(int correctAnswerCount, int totalQuestions)
    {
        if (totalQuestions == 0)
        {
            _logger.LogWarning("ValidateKnowledgeCheckScore called with totalQuestions = 0");
            return false;
        }

        var percentage = CalculateKnowledgeCheckPercentage(correctAnswerCount, totalQuestions);
        var isPassed = percentage >= (KNOWLEDGE_CHECK_PASSING_THRESHOLD * 100);

        _logger.LogInformation(
            "Knowledge check validation: {CorrectAnswers}/{TotalQuestions} = {Percentage}% - Passed: {IsPassed}",
            correctAnswerCount, totalQuestions, percentage, isPassed);

        return isPassed;
    }

    public decimal CalculateKnowledgeCheckPercentage(int correctAnswerCount, int totalQuestions)
    {
        if (totalQuestions == 0)
        {
            return 0;
        }

        var percentage = (decimal)correctAnswerCount / totalQuestions * 100;
        return Math.Round(percentage, 2);
    }

    public (bool isPassed, decimal percentage) EvaluateKnowledgeCheckSubmission(int correctAnswerCount, int totalQuestions)
    {
        var percentage = CalculateKnowledgeCheckPercentage(correctAnswerCount, totalQuestions);
        var isPassed = ValidateKnowledgeCheckScore(correctAnswerCount, totalQuestions);

        _logger.LogInformation(
            "Knowledge check submission evaluated - Score: {Percentage}%, Passed: {IsPassed}",
            percentage, isPassed);

        return (isPassed, percentage);
    }
}
