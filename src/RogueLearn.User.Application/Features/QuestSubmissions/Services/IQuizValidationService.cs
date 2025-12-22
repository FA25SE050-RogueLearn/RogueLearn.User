namespace RogueLearn.User.Application.Features.QuestSubmissions.Services;

public interface IQuizValidationService
{
    /// <summary>
    /// Validates quiz answers and returns whether the user passed (70% threshold).
    /// </summary>
    /// <param name="correctAnswerCount">Number of correct answers</param>
    /// <param name="totalQuestions">Total number of questions in the quiz</param>
    /// <returns>True if score >= 70%, false otherwise</returns>
    bool ValidateQuizScore(int correctAnswerCount, int totalQuestions);

    /// <summary>
    /// Calculates the percentage score for a quiz.
    /// </summary>
    decimal CalculateQuizPercentage(int correctAnswerCount, int totalQuestions);

    /// <summary>
    /// Determines if a quiz submission meets passing criteria.
    /// </summary>
    (bool isPassed, decimal percentage) EvaluateQuizSubmission(int correctAnswerCount, int totalQuestions);
}
