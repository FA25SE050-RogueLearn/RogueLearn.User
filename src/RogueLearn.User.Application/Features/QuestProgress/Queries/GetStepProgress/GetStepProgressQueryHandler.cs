// RogueLearn.User/src/RogueLearn.User.Application/Features/QuestProgress/Queries/GetStepProgress/GetStepProgressQueryHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.QuestProgress.Queries.GetStepProgress;

public class GetStepProgressQueryHandler : IRequestHandler<GetStepProgressQuery, GetStepProgressResponse>
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
            // 1. Verify quest step exists
            var questStep = await _questStepRepository.GetByIdAsync(request.StepId, cancellationToken);
            if (questStep is null || questStep.QuestId != request.QuestId)
            {
                _logger.LogWarning("❌ QuestStep {StepId} not found or belongs to different quest", request.StepId);
                throw new NotFoundException("QuestStep", request.StepId);
            }

            // 2. Get user's quest attempt
            var attempt = await _attemptRepository.FirstOrDefaultAsync(
                a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
                cancellationToken);

            if (attempt is null)
            {
                _logger.LogWarning("❌ UserQuestAttempt not found for User:{UserId}, Quest:{QuestId}",
                    request.AuthUserId, request.QuestId);
                throw new NotFoundException("UserQuestAttempt", request.QuestId);
            }

            // 3. ⭐ FIX: Get step progress - if null, return empty progress (user just started this step)
            var stepProgress = await _stepProgressRepository.FirstOrDefaultAsync(
                sp => sp.AttemptId == attempt.Id && sp.StepId == request.StepId,
                cancellationToken);

            if (stepProgress is null)
            {
                _logger.LogInformation("ℹ️ No progress yet for Step:{StepId} - user just started this step", request.StepId);

                // Count total activities in step
                var totalActivities = ExtractActivityCount(questStep.Content);

                // Return empty progress with 0 completed activities
                var emptyResponse = new GetStepProgressResponse
                {
                    StepId = questStep.Id,
                    StepTitle = questStep.Title,
                    Status = "InProgress",
                    CompletedActivitiesCount = 0,
                    TotalActivitiesCount = totalActivities,
                    StartedAt = null,
                    CompletedAt = null,
                    CompletedActivityIds = Array.Empty<Guid>(),
                    ProgressPercentage = 0
                };

                _logger.LogInformation("📊 Returned empty progress: 0/{Total} activities", totalActivities);

                return emptyResponse;
            }

            // 4. Count total activities in step
            var totalActivitiesCount = ExtractActivityCount(questStep.Content);
            var completedCount = stepProgress.CompletedActivityIds?.Length ?? 0;

            var progressPercentage = totalActivitiesCount > 0
                ? Math.Round((decimal)completedCount / totalActivitiesCount * 100, 2)
                : 0;

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

    private int ExtractActivityCount(object? content)
    {
        if (content == null) return 0;

        try
        {
            // Handle JObject from EF Core JSONB
            if (content.GetType().Name == "JObject")
            {
                var jObjectJson = content.ToString();
                using (var doc = JsonDocument.Parse(jObjectJson))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("activities", out var activitiesElement) &&
                        activitiesElement.ValueKind == JsonValueKind.Array)
                    {
                        return activitiesElement.GetArrayLength();
                    }
                }
                return 0;
            }

            // Handle Dictionary format
            if (content is Dictionary<string, object> contentDict &&
                contentDict.TryGetValue("activities", out var activitiesObj) &&
                activitiesObj is List<object> activitiesList)
            {
                return activitiesList.Count;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error extracting activity count");
        }

        return 0;
    }
}