// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Services/ActivityValidationService.cs
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Quests.Services;

/// <summary>
/// Validates whether an activity can be marked as complete.
/// 
/// Validation Rules:
/// - Reading: Always can be completed (no validation needed)
/// - Coding: Always can be completed (separate grading system)
/// - KnowledgeCheck: Always can be completed (formative assessment, local validation only)
/// - Quiz: MUST have passing submission record (summative assessment, requires 70%+ score)
/// </summary>
public class ActivityValidationService
{
    private readonly IQuestSubmissionRepository _submissionRepository;
    private readonly ILogger<ActivityValidationService> _logger;

    public ActivityValidationService(
        IQuestSubmissionRepository submissionRepository,
        ILogger<ActivityValidationService> logger)
    {
        _submissionRepository = submissionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Validates if an activity can be marked as complete based on type and submission status.
    /// </summary>
    /// <param name="activityId">The activity ID to validate</param>
    /// <param name="userId">The user attempting to complete the activity</param>
    /// <param name="activityType">The type of activity (Quiz, KnowledgeCheck, Reading, Coding)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (canComplete, message)</returns>
    public async Task<(bool canComplete, string message)> ValidateActivityCompletion(
        Guid activityId,
        Guid userId,
        string activityType,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "🔍 Validating {ActivityType} completion for activity {ActivityId} by user {UserId}",
            activityType, activityId, userId);

        // ⭐ OPTION B: Different validation rules per activity type
        switch (activityType)
        {
            case "Quiz":
                return await ValidateQuizCompletion(activityId, userId, cancellationToken);

            case "KnowledgeCheck":
                return ValidateKnowledgeCheckCompletion();

            case "Reading":
                return ValidateReadingCompletion();

            case "Coding":
                return ValidateCodingCompletion();

            default:
                _logger.LogWarning("⚠️ Unknown activity type: {ActivityType}", activityType);
                return (true, "Activity completed");
        }
    }

    /// <summary>
    /// Validates Quiz completion.
    /// 
    /// Requirements:
    /// - Must have a submission record
    /// - Submission must be marked as passed (IsPassed = true)
    /// - Score must be >= 70% (enforced by backend when creating submission)
    /// </summary>
    private async Task<(bool canComplete, string message)> ValidateQuizCompletion(
        Guid activityId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("📋 Validating Quiz for activity {ActivityId}", activityId);

        // Get latest submission for this activity
        var submission = await _submissionRepository.GetLatestByActivityAndUserAsync(
            activityId, userId, cancellationToken);

        if (submission == null)
        {
            _logger.LogWarning(
                "❌ No submission found for Quiz activity {ActivityId} by user {UserId}",
                activityId, userId);
            return (false, "Quiz must be submitted first");
        }

        if (!submission.IsPassed.HasValue)
        {
            _logger.LogWarning(
                "❌ Quiz submission exists but IsPassed is null for activity {ActivityId}",
                activityId);
            return (false, "Quiz submission status is invalid");
        }

        if (!submission.IsPassed.Value)
        {
            _logger.LogWarning(
                "❌ Quiz failed for activity {ActivityId} - Score: {Score}/{MaxScore} (Need 70%+)",
                activityId, submission.Grade, submission.MaxGrade);
            return (false, $"Quiz not passed. Score: {submission.Grade}/{submission.MaxGrade}. Required: 70%+");
        }

        _logger.LogInformation(
            "✅ Quiz validation passed - Score: {Score}/{MaxScore}",
            submission.Grade, submission.MaxGrade);
        return (true, "Quiz passed");
    }

    /// <summary>
    /// Validates KnowledgeCheck completion.
    /// 
    /// Requirements:
    /// - None! KnowledgeCheck is formative assessment
    /// - Validation happens locally in the component
    /// - User can always complete after viewing
    /// 
    /// Why no submission record?
    /// - KnowledgeCheck is for learning/practice, not grading
    /// - Immediate feedback is provided in the component
    /// - No need to store results in database
    /// </summary>
    private (bool canComplete, string message) ValidateKnowledgeCheckCompletion()
    {
        _logger.LogInformation("✅ KnowledgeCheck validation passed (formative assessment - no submission needed)");
        return (true, "Knowledge check completed");
    }

    /// <summary>
    /// Validates Reading completion.
    /// 
    /// Requirements:
    /// - None! Reading material can always be marked as complete
    /// - No submission or scoring needed
    /// </summary>
    private (bool canComplete, string message) ValidateReadingCompletion()
    {
        _logger.LogInformation("✅ Reading validation passed (no validation required)");
        return (true, "Reading material completed");
    }

    /// <summary>
    /// Validates Coding challenge completion.
    /// 
    /// Requirements:
    /// - None! Coding challenges are handled by separate system
    /// - User can mark complete when they finish the challenge
    /// </summary>
    private (bool canComplete, string message) ValidateCodingCompletion()
    {
        _logger.LogInformation("✅ Coding challenge validation passed");
        return (true, "Coding challenge completed");
    }
}
