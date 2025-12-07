// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Queries/GetQuestProgress/GetQuestProgressQueryHandler.cs
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetQuestProgress;

public class GetQuestProgressQueryHandler : IRequestHandler<GetQuestProgressQuery, List<QuestStepProgressDto>>
{
    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly IQuestStepRepository _stepRepository;
    private readonly IUserQuestStepProgressRepository _progressRepository;

    public GetQuestProgressQueryHandler(
        IUserQuestAttemptRepository attemptRepository,
        IQuestStepRepository stepRepository,
        IUserQuestStepProgressRepository progressRepository)
    {
        _attemptRepository = attemptRepository;
        _stepRepository = stepRepository;
        _progressRepository = progressRepository;
    }

    public async Task<List<QuestStepProgressDto>> Handle(GetQuestProgressQuery request, CancellationToken cancellationToken)
    {
        // 1. Get the User's Attempt (Source of Truth for Difficulty)
        var attempt = await _attemptRepository.FirstOrDefaultAsync(
            a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
            cancellationToken);

        if (attempt == null)
        {
            throw new NotFoundException("Quest not started. No progress available.");
        }

        // 2. Get All Master Steps
        var allSteps = await _stepRepository.GetByQuestIdAsync(request.QuestId, cancellationToken);

        // 3. Filter Steps by Assigned Difficulty
        var userTrackSteps = allSteps
            .Where(s => string.Equals(s.DifficultyVariant, attempt.AssignedDifficulty, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.StepNumber)
            .ToList();

        if (!userTrackSteps.Any())
        {
            // Fallback: If for some reason the track is empty (e.g., generation failed for that variant),
            // maybe fallback to Standard? For now, return empty to indicate error state.
            return new List<QuestStepProgressDto>();
        }

        // 4. Get Existing Progress Records
        var progressRecords = await _progressRepository.GetByAttemptIdAsync(attempt.Id, cancellationToken);
        var progressDict = progressRecords.ToDictionary(p => p.StepId);

        var result = new List<QuestStepProgressDto>();
        bool previousStepCompleted = true; // First step is unlocked by default

        foreach (var step in userTrackSteps)
        {
            var dto = new QuestStepProgressDto
            {
                StepId = step.Id,
                StepNumber = step.StepNumber,
                Title = step.Title,
                DifficultyVariant = step.DifficultyVariant,
                TotalActivitiesCount = CountTotalActivities(step)
            };

            if (progressDict.TryGetValue(step.Id, out var progress))
            {
                dto.Status = progress.Status;
                dto.CompletedActivitiesCount = progress.CompletedActivityIds?.Length ?? 0;
            }
            else
            {
                dto.Status = StepCompletionStatus.NotStarted;
                dto.CompletedActivitiesCount = 0;
            }

            // Lock Logic: Locked if previous step is NOT completed AND this step is NotStarted
            if (!previousStepCompleted && dto.Status == StepCompletionStatus.NotStarted)
            {
                dto.IsLocked = true;
            }
            else
            {
                dto.IsLocked = false;
            }

            // Update for next iteration
            previousStepCompleted = (dto.Status == StepCompletionStatus.Completed);

            result.Add(dto);
        }

        return result;
    }

    private int CountTotalActivities(QuestStep step)
    {
        try
        {
            if (step.Content == null) return 0;
            var jsonString = JsonSerializer.Serialize(step.Content);
            using var doc = JsonDocument.Parse(jsonString);
            if (doc.RootElement.TryGetProperty("activities", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                return arr.GetArrayLength();
            }
        }
        catch { /* ignore */ }
        return 0;
    }
}