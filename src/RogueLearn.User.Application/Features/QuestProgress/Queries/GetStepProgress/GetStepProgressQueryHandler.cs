// RogueLearn.User/src/RogueLearn.User.Application/Features/QuestProgress/Queries/GetStepProgress/GetStepProgressQueryHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.QuestProgress.Queries.GetStepProgress;

public class GetStepProgressQueryHandler : IRequestHandler<GetStepProgressQuery, GetStepProgressResponse?>
{
    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly IUserQuestStepProgressRepository _stepProgressRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly ILogger<GetStepProgressQueryHandler> _logger;

    public GetStepProgressQueryHandler(
        IUserQuestAttemptRepository attemptRepository,
        IUserQuestStepProgressRepository stepProgressRepository,
        IQuestStepRepository questStepRepository,
        ILogger<GetStepProgressQueryHandler> logger)
    {
        _attemptRepository = attemptRepository;
        _stepProgressRepository = stepProgressRepository;
        _questStepRepository = questStepRepository;
        _logger = logger;
    }

    public async Task<GetStepProgressResponse?> Handle(GetStepProgressQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔍 Fetching step progress for User:{UserId}, Step:{StepId}",
            request.AuthUserId, request.StepId);

        try
        {
            // 1. Get user's quest attempt FIRST to know the locked track
            var attempt = await _attemptRepository.FirstOrDefaultAsync(
                a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
                cancellationToken);

            if (attempt is null)
            {
                _logger.LogWarning("❌ UserQuestAttempt not found for User:{UserId}, Quest:{QuestId}",
                    request.AuthUserId, request.QuestId);
                throw new NotFoundException("UserQuestAttempt", request.QuestId);
            }

            // 2. Verify quest step exists AND matches the locked difficulty
            var questStep = await _questStepRepository.GetByIdAsync(request.StepId, cancellationToken);
            if (questStep is null || questStep.QuestId != request.QuestId)
            {
                _logger.LogWarning("❌ QuestStep {StepId} not found or belongs to different quest", request.StepId);
                throw new NotFoundException("QuestStep", request.StepId);
            }

            // ⭐ CRITICAL VALIDATION: Ensure Step matches User's Locked Difficulty
            string lockedDifficulty = attempt.AssignedDifficulty ?? "Standard";
            if (!string.Equals(questStep.DifficultyVariant, lockedDifficulty, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("❌ Mismatch: User is locked to '{Locked}', but requested Step {StepId} is '{StepVariant}'",
                    lockedDifficulty, questStep.DifficultyVariant);

                // You can either throw NotFound (to hide it) or BadRequest (to debug it)
                // Returning a specific error helps the frontend know it requested the wrong track
                throw new BadRequestException($"This step belongs to the '{questStep.DifficultyVariant}' track, but you are locked to '{lockedDifficulty}'.");
            }

            // 3. Get step progress
            var stepProgress = await _stepProgressRepository.FirstOrDefaultAsync(
                sp => sp.AttemptId == attempt.Id && sp.StepId == request.StepId,
                cancellationToken);

            int totalActivitiesCount = ExtractActivityCount(questStep.Content);

            if (stepProgress is null)
            {
                _logger.LogInformation("ℹ️ No progress yet for Step:{StepId} - user just started this step", request.StepId);

                return new GetStepProgressResponse
                {
                    StepId = questStep.Id,
                    StepTitle = questStep.Title,
                    Status = "InProgress", // Default to InProgress if they can access it
                    CompletedActivitiesCount = 0,
                    TotalActivitiesCount = totalActivitiesCount,
                    StartedAt = null,
                    CompletedAt = null,
                    CompletedActivityIds = Array.Empty<Guid>(),
                    ProgressPercentage = 0
                };
            }

            // 4. Calculate progress
            var completedCount = stepProgress.CompletedActivityIds?.Length ?? 0;
            var progressPercentage = totalActivitiesCount > 0
                ? Math.Round((decimal)completedCount / totalActivitiesCount * 100, 2)
                : 0;

            if (stepProgress.Status == Domain.Enums.StepCompletionStatus.Completed)
            {
                progressPercentage = 100;
            }

            _logger.LogInformation("✅ Step progress: {Completed}/{Total} activities ({Percentage}%)",
                completedCount, totalActivitiesCount, progressPercentage);

            return new GetStepProgressResponse
            {
                StepId = questStep.Id,
                StepTitle = questStep.Title,
                Status = stepProgress.Status.ToString(),
                CompletedActivitiesCount = completedCount,
                TotalActivitiesCount = totalActivitiesCount,
                StartedAt = stepProgress.StartedAt?.DateTime,
                CompletedAt = stepProgress.CompletedAt?.DateTime,
                CompletedActivityIds = stepProgress.CompletedActivityIds ?? Array.Empty<Guid>(),
                ProgressPercentage = progressPercentage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error fetching step progress for Step:{StepId}", request.StepId);
            throw;
        }
    }

    // ... [ExtractJsonString and ExtractActivityCount helpers remain the same]
    private static string ExtractJsonString(object? content)
    {
        if (content is null) return "{}";
        if (content is string s) return s;
        if (content is JsonElement je) return je.GetRawText();
        var typeName = content.GetType().Name;
        if (typeName == "JObject" || typeName == "JArray" || typeName == "JToken") return content.ToString()!;
        return JsonSerializer.Serialize(content);
    }

    private int ExtractActivityCount(object? content)
    {
        try
        {
            var jsonString = ExtractJsonString(content);
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array) return root.GetArrayLength();
            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in root.EnumerateObject())
                {
                    if (string.Equals(prop.Name, "activities", StringComparison.OrdinalIgnoreCase))
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Array) return prop.Value.GetArrayLength();
                    }
                }
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "❌ Error extracting activity count"); }
        return 0;
    }
}