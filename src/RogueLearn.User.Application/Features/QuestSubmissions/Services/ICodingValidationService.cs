namespace RogueLearn.User.Application.Features.QuestSubmissions.Services;

public interface ICodingValidationService
{
    /// <summary>
    /// Uses AI to evaluate student code against the activity requirements.
    /// </summary>
    /// <param name="studentCode">The code written by the user.</param>
    /// <param name="language">The programming language.</param>
    /// <param name="description">The problem description.</param>
    /// <param name="validationCriteria">Hidden criteria generated during quest creation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing pass status, score, and feedback.</returns>
    Task<(bool isPassed, int score, string feedback)> EvaluateCodeAsync(
        string studentCode,
        string language,
        string description,
        string? validationCriteria,
        CancellationToken cancellationToken);
}