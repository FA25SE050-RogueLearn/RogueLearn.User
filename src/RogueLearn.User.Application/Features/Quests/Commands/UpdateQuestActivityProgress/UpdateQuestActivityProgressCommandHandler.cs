// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/UpdateQuestActivityProgress/UpdateQuestActivityProgressCommandHandler.cs
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestActivityProgress;

public class UpdateQuestActivityProgressCommandHandler : IRequestHandler<UpdateQuestActivityProgressCommand>
{
    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly IUserQuestStepProgressRepository _stepProgressRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly ILogger<UpdateQuestActivityProgressCommandHandler> _logger;

    public UpdateQuestActivityProgressCommandHandler(
        IUserQuestAttemptRepository attemptRepository,
        IUserQuestStepProgressRepository stepProgressRepository,
        IQuestStepRepository questStepRepository,
        ILogger<UpdateQuestActivityProgressCommandHandler> logger)
    {
        _attemptRepository = attemptRepository;
        _stepProgressRepository = stepProgressRepository;
        _questStepRepository = questStepRepository;
        _logger = logger;
    }

    public async Task Handle(UpdateQuestActivityProgressCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch the Quest Step
        var step = await _questStepRepository.GetByIdAsync(request.StepId, cancellationToken);
        if (step == null)
        {
            throw new NotFoundException($"Quest Step {request.StepId} not found");
        }

        // 2. Fetch the User's Quest Attempt (Must already exist)
        // REMOVED LAZY CREATION: This endpoint now expects the quest to be started explicitly
        var attempt = await _attemptRepository.FirstOrDefaultAsync(
            a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
            cancellationToken);

        if (attempt == null)
        {
            _logger.LogWarning("User {UserId} tried to update progress for Quest {QuestId} but hasn't started it.", request.AuthUserId, request.QuestId);
            throw new NotFoundException("Quest not started. Please start the quest before tracking progress.");
        }

        // 3. Verify Track Match
        if (!string.Equals(step.DifficultyVariant, attempt.AssignedDifficulty, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Difficulty mismatch: Step is {StepVar}, User is {UserVar}.", step.DifficultyVariant, attempt.AssignedDifficulty);
        }

        // 4. Get or Create Step Progress (In Memory)
        var stepProgress = await _stepProgressRepository.FirstOrDefaultAsync(
            p => p.AttemptId == attempt.Id && p.StepId == request.StepId,
            cancellationToken);

        bool isNewStepProgress = false;

        if (stepProgress == null)
        {
            isNewStepProgress = true;
            stepProgress = new UserQuestStepProgress
            {
                Id = Guid.NewGuid(),
                AttemptId = attempt.Id,
                StepId = request.StepId,
                Status = StepCompletionStatus.InProgress,
                CompletedActivityIds = new Guid[] { },
                StartedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }

        var completedList = stepProgress.CompletedActivityIds != null
            ? stepProgress.CompletedActivityIds.ToList()
            : new List<Guid>();

        bool isAlreadyCompleted = completedList.Contains(request.ActivityId);
        bool progressChanged = false;

        if (request.Status == StepCompletionStatus.Completed && !isAlreadyCompleted)
        {
            completedList.Add(request.ActivityId);
            stepProgress.CompletedActivityIds = completedList.ToArray();
            progressChanged = true;

            // Mastery Override Logic
            bool isQuiz = IsQuizActivity(step, request.ActivityId);
            bool isCompleteByCount = CheckIfStepIsComplete(step, completedList);

            if (isQuiz || isCompleteByCount)
            {
                _logger.LogInformation("Step {StepId} COMPLETE. Reason: {Reason}", step.Id, isQuiz ? "Quiz Mastery" : "All Activities Done");

                stepProgress.Status = StepCompletionStatus.Completed;
                stepProgress.CompletedAt = DateTimeOffset.UtcNow;

                attempt.TotalExperienceEarned += step.ExperiencePoints;
                await _attemptRepository.UpdateAsync(attempt, cancellationToken);
            }
            else
            {
                stepProgress.Status = StepCompletionStatus.InProgress;
            }
        }
        else if (request.Status != StepCompletionStatus.Completed && isAlreadyCompleted)
        {
            completedList.Remove(request.ActivityId);
            stepProgress.CompletedActivityIds = completedList.ToArray();
            progressChanged = true;

            stepProgress.Status = StepCompletionStatus.InProgress;
            stepProgress.CompletedAt = null;
        }

        // 5. Persist Changes (Single DB Operation)
        if (isNewStepProgress)
        {
            stepProgress.UpdatedAt = DateTimeOffset.UtcNow;
            await _stepProgressRepository.AddAsync(stepProgress, cancellationToken);
            _logger.LogInformation("Created NEW progress record for Step {StepId}", stepProgress.StepId);
        }
        else if (progressChanged)
        {
            stepProgress.UpdatedAt = DateTimeOffset.UtcNow;
            await _stepProgressRepository.UpdateAsync(stepProgress, cancellationToken);
            _logger.LogInformation("Updated EXISTING progress record for Step {StepId}", stepProgress.StepId);
        }
    }

    private static string ExtractJsonString(object? content)
    {
        if (content is null) return "{}";
        if (content is string s) return s;
        if (content is JsonElement je) return je.GetRawText();

        var typeName = content.GetType().Name;
        if (typeName == "JObject" || typeName == "JArray" || typeName == "JToken")
            return content.ToString()!;

        return JsonSerializer.Serialize(content);
    }

    private static JsonElement? TryGetActivitiesElement(JsonDocument doc)
    {
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Array) return root;
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in root.EnumerateObject())
            {
                if (string.Equals(prop.Name, "activities", StringComparison.OrdinalIgnoreCase))
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array) return prop.Value;
                }
            }
        }
        return null;
    }

    private bool IsQuizActivity(QuestStep step, Guid activityId)
    {
        try
        {
            var jsonString = ExtractJsonString(step.Content);
            using var doc = JsonDocument.Parse(jsonString);

            var activitiesElement = TryGetActivitiesElement(doc);
            if (activitiesElement == null) return false;

            foreach (var activity in activitiesElement.Value.EnumerateArray())
            {
                if (activity.ValueKind == JsonValueKind.Object &&
                    activity.TryGetProperty("activityId", out var idEl) &&
                    Guid.TryParse(idEl.GetString(), out var id) &&
                    id == activityId)
                {
                    if (activity.TryGetProperty("type", out var typeEl))
                    {
                        var type = typeEl.GetString();
                        return string.Equals(type, "Quiz", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if activity {ActivityId} is quiz", activityId);
        }
        return false;
    }

    private bool CheckIfStepIsComplete(QuestStep step, List<Guid> completedActivityIds)
    {
        try
        {
            var jsonString = ExtractJsonString(step.Content);
            using var doc = JsonDocument.Parse(jsonString);

            var activitiesElement = TryGetActivitiesElement(doc);

            if (activitiesElement.HasValue && activitiesElement.Value.ValueKind == JsonValueKind.Array)
            {
                int totalActivities = activitiesElement.Value.GetArrayLength();
                int matchedCount = 0;

                foreach (var activity in activitiesElement.Value.EnumerateArray())
                {
                    if (activity.ValueKind == JsonValueKind.Object &&
                        activity.TryGetProperty("activityId", out var idElement) &&
                        Guid.TryParse(idElement.GetString(), out var activityGuid))
                    {
                        if (completedActivityIds.Contains(activityGuid))
                        {
                            matchedCount++;
                        }
                    }
                }
                return matchedCount >= totalActivities;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking step completion for Step {StepId}", step.Id);
        }
        return false;
    }
}