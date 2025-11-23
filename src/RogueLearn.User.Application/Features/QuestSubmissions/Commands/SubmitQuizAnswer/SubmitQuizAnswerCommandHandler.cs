// RogueLearn.User/src/RogueLearn.User.Application/Features/QuestSubmissions/Commands/SubmitQuizAnswer/SubmitQuizAnswerCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Quests.Services;
using RogueLearn.User.Application.Features.QuestSubmissions.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.QuestSubmissions.Commands.SubmitQuizAnswer;

public class SubmitQuizAnswerCommandHandler : IRequestHandler<SubmitQuizAnswerCommand, SubmitQuizAnswerResponse>
{
    private readonly IQuestSubmissionRepository _submissionRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IUserQuestAttemptRepository _userQuestAttemptRepository;
    private readonly IQuizValidationService _quizValidationService;
    private readonly ILogger<SubmitQuizAnswerCommandHandler> _logger;

    public SubmitQuizAnswerCommandHandler(
        IQuestSubmissionRepository submissionRepository,
        IQuestStepRepository questStepRepository,
        IQuestRepository questRepository,
        IUserQuestAttemptRepository userQuestAttemptRepository,
        IQuizValidationService quizValidationService,
        ILogger<SubmitQuizAnswerCommandHandler> logger)
    {
        _submissionRepository = submissionRepository;
        _questStepRepository = questStepRepository;
        _questRepository = questRepository;
        _quizValidationService = quizValidationService;
        _userQuestAttemptRepository = userQuestAttemptRepository;
        _logger = logger;
    }

    public async Task<SubmitQuizAnswerResponse> Handle(
        SubmitQuizAnswerCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing quiz submission for User:{UserId}, Quest:{QuestId}, Step:{StepId}, Activity:{ActivityId}",
            request.AuthUserId, request.QuestId, request.StepId, request.ActivityId);

        try
        {
            // ========== VALIDATION ==========

            var quest = await _questRepository.GetByIdAsync(request.QuestId, cancellationToken);
            if (quest == null)
            {
                _logger.LogError("Quest {QuestId} not found", request.QuestId);
                throw new NotFoundException("Quest", request.QuestId);
            }

            var step = await _questStepRepository.GetByIdAsync(request.StepId, cancellationToken);
            if (step == null || step.QuestId != request.QuestId)
            {
                _logger.LogError("Quest step {StepId} not found in quest {QuestId}", request.StepId, request.QuestId);
                throw new NotFoundException("Quest step", request.StepId);
            }

            // ⭐ NEW: Get or create the attempt
            var attempt = await _userQuestAttemptRepository.FirstOrDefaultAsync(
                a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
                cancellationToken);

            if (attempt == null)
            {
                _logger.LogInformation("Creating new attempt for User {UserId}, Quest {QuestId}", request.AuthUserId, request.QuestId);
                attempt = new UserQuestAttempt
                {
                    AuthUserId = request.AuthUserId,
                    QuestId = request.QuestId,
                    Status = QuestAttemptStatus.InProgress,
                    StartedAt = DateTimeOffset.UtcNow
                };
                attempt = await _userQuestAttemptRepository.AddAsync(attempt, cancellationToken);
            }

            _logger.LogInformation("Using attempt {AttemptId} for quiz submission", attempt.Id);

            // Extract activity type from step content
            var activityType = ExtractActivityType(step.Content, request.ActivityId);
            _logger.LogInformation("Extracted activity type: {Type} for activity {ActivityId}", activityType, request.ActivityId);

            if (activityType == "Unknown")
            {
                _logger.LogError("Activity {ActivityId} not found in step {StepId}", request.ActivityId, request.StepId);
                throw new NotFoundException("Activity", request.ActivityId);
            }

            // ========== GRADING ==========

            var (isPassed, scorePercentage) = _quizValidationService.EvaluateQuizSubmission(
                request.CorrectAnswerCount,
                request.TotalQuestions);

            _logger.LogInformation(
                "Quiz graded - Activity:{ActivityId}, Type:{Type}, Score:{Score}%, Passed:{IsPassed}",
                request.ActivityId, activityType, scorePercentage, isPassed);

            // ========== STORE SUBMISSION ==========

            var submission = new QuestSubmission
            {
                Id = Guid.NewGuid(),
                UserId = request.AuthUserId,
                QuestId = request.QuestId,
                StepId = request.StepId,
                ActivityId = request.ActivityId,
                AttemptId = attempt.Id, // ⭐ USE THE EXISTING ATTEMPT ID
                SubmissionData = JsonSerializer.Serialize(request.Answers),
                Grade = scorePercentage,
                MaxGrade = request.TotalQuestions,
                IsPassed = isPassed,
                SubmittedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            var savedSubmission = await _submissionRepository.AddAsync(submission, cancellationToken);

            _logger.LogInformation(
                "Quiz submission saved - SubmissionId:{SubmissionId}, Activity:{ActivityId}, Score:{Score}%, AttemptId:{AttemptId}",
                savedSubmission.Id, request.ActivityId, scorePercentage, attempt.Id);

            // ========== RETURN RESPONSE ==========

            return new SubmitQuizAnswerResponse
            {
                SubmissionId = savedSubmission.Id,
                CorrectAnswerCount = request.CorrectAnswerCount,
                TotalQuestions = request.TotalQuestions,
                ScorePercentage = Math.Round(scorePercentage, 2),
                IsPassed = isPassed,
                Message = isPassed
                    ? $"✅ Congratulations! You scored {Math.Round(scorePercentage, 2)}%. {activityType} passed!"
                    : $"❌ {activityType} score: {Math.Round(scorePercentage, 2)}%. You need {(activityType == "Quiz" ? "70%" : "100%")} to pass. Please try again.",
                CanCompleteActivity = isPassed
            };
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing quiz submission for Activity:{ActivityId}", request.ActivityId);
            throw;
        }
    }


    /// <summary>
    /// Extracts activity type from step content JSON.
    /// </summary>
    private string ExtractActivityType(object stepContent, Guid activityId)
    {
        try
        {
            if (stepContent == null)
            {
                _logger.LogWarning("Step content is NULL for activity {ActivityId}", activityId);
                return "Unknown";
            }

            string jsonString = null;

            // Handle different content types
            if (stepContent is string strContent)
            {
                if (string.IsNullOrWhiteSpace(strContent))
                {
                    _logger.LogWarning("Step content string is empty for activity {ActivityId}", activityId);
                    return "Unknown";
                }
                jsonString = strContent;
            }
            else if (stepContent is Dictionary<string, object> dictContent)
            {
                jsonString = JsonSerializer.Serialize(dictContent);
            }
            else if (stepContent.GetType().Name == "JObject")
            {
                jsonString = stepContent.ToString();
            }
            else
            {
                _logger.LogWarning("Content is unsupported type: {Type}", stepContent.GetType().FullName);
                return "Unknown";
            }

            if (string.IsNullOrWhiteSpace(jsonString))
            {
                _logger.LogWarning("JSON string is empty after conversion for activity {ActivityId}", activityId);
                return "Unknown";
            }

            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                var root = doc.RootElement;

                if (!root.TryGetProperty("activities", out var activitiesElement) ||
                    activitiesElement.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning("'activities' property not found or not array for activity {ActivityId}", activityId);
                    return "Unknown";
                }

                foreach (var activityElement in activitiesElement.EnumerateArray())
                {
                    if (activityElement.ValueKind != JsonValueKind.Object)
                        continue;

                    if (!activityElement.TryGetProperty("activityId", out var idElement) ||
                        !Guid.TryParse(idElement.GetString(), out var id))
                        continue;

                    if (id != activityId)
                        continue;

                    if (activityElement.TryGetProperty("type", out var typeElement))
                    {
                        var activityType = typeElement.GetString() ?? "Unknown";
                        _logger.LogInformation("Extracted activity type: {Type} for activity {ActivityId}", activityType, activityId);
                        return activityType;
                    }
                }

                _logger.LogWarning("Activity {ActivityId} not found in activities array", activityId);
                return "Unknown";
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for activity {ActivityId}", activityId);
            return "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error extracting activity type for {ActivityId}", activityId);
            return "Unknown";
        }
    }
}
