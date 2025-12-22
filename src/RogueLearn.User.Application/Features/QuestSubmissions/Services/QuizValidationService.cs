using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.QuestSubmissions.Services;

public class QuizValidationService : IQuizValidationService
{
    private const decimal QUIZ_PASSING_THRESHOLD = 0.70m; // 70%
    private readonly ILogger<QuizValidationService> _logger;

    public QuizValidationService(ILogger<QuizValidationService> logger)
    {
        _logger = logger;
    }

    public bool ValidateQuizScore(int correctAnswerCount, int totalQuestions)
    {
        if (totalQuestions == 0)
        {
            _logger.LogWarning("ValidateQuizScore called with totalQuestions = 0");
            return false;
        }

        var percentage = CalculateQuizPercentage(correctAnswerCount, totalQuestions);
        var isPassed = percentage >= (QUIZ_PASSING_THRESHOLD * 100);

        _logger.LogInformation(
            "Quiz validation: {CorrectAnswers}/{TotalQuestions} = {Percentage}% - Passed: {IsPassed}",
            correctAnswerCount, totalQuestions, percentage, isPassed);

        return isPassed;
    }

    public decimal CalculateQuizPercentage(int correctAnswerCount, int totalQuestions)
    {
        if (totalQuestions == 0)
        {
            return 0;
        }

        var percentage = (decimal)correctAnswerCount / totalQuestions * 100;
        return Math.Round(percentage, 2);
    }

    public (bool isPassed, decimal percentage) EvaluateQuizSubmission(int correctAnswerCount, int totalQuestions)
    {
        var percentage = CalculateQuizPercentage(correctAnswerCount, totalQuestions);
        var isPassed = ValidateQuizScore(correctAnswerCount, totalQuestions);

        _logger.LogInformation(
            "Quiz submission evaluated - Score: {Percentage}%, Passed: {IsPassed}",
            percentage, isPassed);

        return (isPassed, percentage);
    }
}
