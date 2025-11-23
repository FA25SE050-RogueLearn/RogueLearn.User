// RogueLearn.User/src/RogueLearn.User.Application/Features/QuestSubmissions/Commands/SubmitQuizAnswer/SubmitQuizAnswerCommand.cs
using MediatR;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.QuestSubmissions.Commands.SubmitQuizAnswer;

public class SubmitQuizAnswerCommand : IRequest<SubmitQuizAnswerResponse>
{
    [JsonIgnore]
    public Guid AuthUserId { get; set; }

    [JsonIgnore]
    public Guid QuestId { get; set; }

    [JsonIgnore]
    public Guid StepId { get; set; }

    [JsonIgnore]
    public Guid ActivityId { get; set; }

    /// <summary>
    /// The user's answers to quiz questions.
    /// Format: Dictionary where key is question ID and value is the answer.
    /// </summary>
    public Dictionary<string, string> Answers { get; set; } = new();

    /// <summary>
    /// Number of questions the user answered correctly (calculated on frontend/backend).
    /// </summary>
    public int CorrectAnswerCount { get; set; }

    /// <summary>
    /// Total number of questions in the quiz (from activity payload).
    /// </summary>
    public int TotalQuestions { get; set; }
}

public class SubmitQuizAnswerResponse
{
    /// <summary>
    /// Unique identifier for this submission.
    /// </summary>
    public Guid SubmissionId { get; set; }

    /// <summary>
    /// Number of correct answers.
    /// </summary>
    public int CorrectAnswerCount { get; set; }

    /// <summary>
    /// Total number of questions.
    /// </summary>
    public int TotalQuestions { get; set; }

    /// <summary>
    /// Score percentage (0-100).
    /// </summary>
    public decimal ScorePercentage { get; set; }

    /// <summary>
    /// Whether the quiz passed (70% threshold for Quiz, 100% for KnowledgeCheck).
    /// </summary>
    public bool IsPassed { get; set; }

    /// <summary>
    /// Message explaining the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Whether the activity can now be marked as complete.
    /// Only true if IsPassed is true.
    /// </summary>
    public bool CanCompleteActivity { get; set; }
}
