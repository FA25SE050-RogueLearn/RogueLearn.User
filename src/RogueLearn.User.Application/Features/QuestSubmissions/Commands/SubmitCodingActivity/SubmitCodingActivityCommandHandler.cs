using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.QuestSubmissions.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.QuestSubmissions.Commands.SubmitCodingActivity;

public class SubmitCodingActivityCommandHandler : IRequestHandler<SubmitCodingActivityCommand, SubmitCodingActivityResponse>
{
    private readonly IQuestSubmissionRepository _submissionRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IUserQuestAttemptRepository _userQuestAttemptRepository;
    private readonly ICodingValidationService _codingValidationService;
    private readonly ILogger<SubmitCodingActivityCommandHandler> _logger;

    public SubmitCodingActivityCommandHandler(
        IQuestSubmissionRepository submissionRepository,
        IQuestStepRepository questStepRepository,
        IQuestRepository questRepository,
        IUserQuestAttemptRepository userQuestAttemptRepository,
        ICodingValidationService codingValidationService,
        ILogger<SubmitCodingActivityCommandHandler> logger)
    {
        _submissionRepository = submissionRepository;
        _questStepRepository = questStepRepository;
        _questRepository = questRepository;
        _userQuestAttemptRepository = userQuestAttemptRepository;
        _codingValidationService = codingValidationService;
        _logger = logger;
    }

    public async Task<SubmitCodingActivityResponse> Handle(SubmitCodingActivityCommand request, CancellationToken cancellationToken)
    {
        // 1. Basic Validation
        var step = await _questStepRepository.GetByIdAsync(request.StepId, cancellationToken)
            ?? throw new NotFoundException("QuestStep", request.StepId);

        // 2. Get/Create Attempt
        var attempt = await _userQuestAttemptRepository.FirstOrDefaultAsync(
            a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
            cancellationToken);

        if (attempt == null)
        {
            attempt = new UserQuestAttempt
            {
                AuthUserId = request.AuthUserId,
                QuestId = request.QuestId,
                Status = QuestAttemptStatus.InProgress,
                StartedAt = DateTimeOffset.UtcNow
            };
            attempt = await _userQuestAttemptRepository.AddAsync(attempt, cancellationToken);
        }

        // 3. Extract Activity Metadata from JSON Content
        var (description, criteria) = ExtractActivityDetails(step.Content, request.ActivityId);

        // 4. AI Grading
        var (isPassed, score, feedback) = await _codingValidationService.EvaluateCodeAsync(
            request.Code,
            request.Language,
            description,
            criteria,
            cancellationToken);

        // 5. Save Submission
        var submission = new QuestSubmission
        {
            Id = Guid.NewGuid(),
            UserId = request.AuthUserId,
            QuestId = request.QuestId,
            StepId = request.StepId,
            ActivityId = request.ActivityId,
            AttemptId = attempt.Id,
            SubmissionData = JsonSerializer.Serialize(new { code = request.Code, language = request.Language }),
            Grade = score,
            MaxGrade = 100,
            IsPassed = isPassed,
            SubmittedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _submissionRepository.AddAsync(submission, cancellationToken);

        return new SubmitCodingActivityResponse
        {
            SubmissionId = submission.Id,
            IsPassed = isPassed,
            Score = score,
            Feedback = feedback
        };
    }

    private (string description, string? criteria) ExtractActivityDetails(object? content, Guid activityId)
    {
        try
        {
            string jsonString = content is string s ? s : JsonSerializer.Serialize(content);
            using var doc = JsonDocument.Parse(jsonString);

            // Check new schema root vs old schema "activities" array
            JsonElement root = doc.RootElement;
            JsonElement activities;

            // Handle structure where root might be array or object
            if (root.ValueKind == JsonValueKind.Array)
                activities = root;
            else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("activities", out var arr))
                activities = arr;
            else
                return ("Code Challenge", null); // Fallback

            if (activities.ValueKind == JsonValueKind.Array)
            {
                foreach (var act in activities.EnumerateArray())
                {
                    if (act.TryGetProperty("activityId", out var idProp) &&
                        Guid.TryParse(idProp.GetString(), out var id) &&
                        id == activityId)
                    {
                        if (act.TryGetProperty("payload", out var payload))
                        {
                            var desc = payload.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                            var crit = payload.TryGetProperty("validationCriteria", out var c) ? c.GetString() : null;
                            return (desc, crit);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract coding details from content");
        }
        return ("Code Challenge", null);
    }
}