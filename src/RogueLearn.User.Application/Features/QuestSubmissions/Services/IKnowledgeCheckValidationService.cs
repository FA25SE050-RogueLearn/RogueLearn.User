namespace RogueLearn.User.Application.Features.QuestSubmissions.Services;

public interface IKnowledgeCheckValidationService
{
    /// <summary>
    /// Validates knowledge check answers and returns whether the user achieved 100% score.
    /// </summary>
    /// <param name="correctAnswerCount">Number of correct answers</param>
    /// <param name="totalQuestions">Total number of questions in the knowledge check</param>
    /// <returns>True if score == 100%, false otherwise</returns>
    bool ValidateKnowledgeCheckScore(int correctAnswerCount, int totalQuestions);

    /// <summary>
    /// Calculates the percentage score for a knowledge check.
    /// </summary>
    decimal CalculateKnowledgeCheckPercentage(int correctAnswerCount, int totalQuestions);

    /// <summary>
    /// Determines if a knowledge check submission meets passing criteria (100% required).
    /// </summary>
    (bool isPassed, decimal percentage) EvaluateKnowledgeCheckSubmission(int correctAnswerCount, int totalQuestions);
}
