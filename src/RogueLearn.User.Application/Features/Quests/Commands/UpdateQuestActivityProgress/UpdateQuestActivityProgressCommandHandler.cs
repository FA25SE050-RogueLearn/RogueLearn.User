// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/UpdateQuestActivityProgress/UpdateQuestActivityProgressCommandHandler.cs
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestActivityProgress;

public class UpdateQuestActivityProgressCommandHandler : IRequestHandler<UpdateQuestActivityProgressCommand>
{
    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly IUserQuestStepProgressRepository _stepProgressRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly IUserSkillRepository _userSkillRepository;
    private readonly ISubjectSkillMappingRepository _subjectSkillMappingRepository;

    public UpdateQuestActivityProgressCommandHandler(
        IUserQuestAttemptRepository attemptRepository,
        IUserQuestStepProgressRepository stepProgressRepository,
        IQuestStepRepository questStepRepository,
        IUserSkillRepository userSkillRepository,
        ISubjectSkillMappingRepository subjectSkillMappingRepository)
    {
        _attemptRepository = attemptRepository;
        _stepProgressRepository = stepProgressRepository;
        _questStepRepository = questStepRepository;
        _userSkillRepository = userSkillRepository;
        _subjectSkillMappingRepository = subjectSkillMappingRepository;
    }

    public async Task Handle(UpdateQuestActivityProgressCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch the Quest Step
        var step = await _questStepRepository.GetByIdAsync(request.StepId, cancellationToken);
        if (step == null)
        {
            throw new NotFoundException($"Quest Step {request.StepId} not found");
        }

        // 2. Fetch the User's Quest Attempt
        var attempt = await _attemptRepository.FirstOrDefaultAsync(
            a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
            cancellationToken);

        if (attempt == null)
        {
            throw new ForbiddenException("You must start this quest before tracking progress.");
        }

        // 3. Validation: Ensure user is working on their assigned track
        if (!string.Equals(step.DifficultyVariant, attempt.AssignedDifficulty, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException(
                $"Invalid track. You are assigned to '{attempt.AssignedDifficulty}' difficulty, " +
                $"but this activity belongs to '{step.DifficultyVariant}'.");
        }

        // 4. Get or Create Step Progress Record
        var stepProgress = await _stepProgressRepository.FirstOrDefaultAsync(
            p => p.AttemptId == attempt.Id && p.StepId == request.StepId,
            cancellationToken);

        if (stepProgress == null)
        {
            stepProgress = new UserQuestStepProgress
            {
                Id = Guid.NewGuid(),
                AttemptId = attempt.Id,
                StepId = request.StepId,
                Status = StepCompletionStatus.NotStarted,
                CompletedActivityIds = new Guid[] { }, // Initialize as empty Guid array
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await _stepProgressRepository.AddAsync(stepProgress, cancellationToken);
        }

        // 5. Update Activity Status logic
        // Convert array to list for manipulation
        var completedList = stepProgress.CompletedActivityIds != null
            ? stepProgress.CompletedActivityIds.ToList()
            : new List<Guid>();

        bool isAlreadyCompleted = completedList.Contains(request.ActivityId);

        if (request.Status == StepCompletionStatus.Completed && !isAlreadyCompleted)
        {
            // Add to completed list
            completedList.Add(request.ActivityId);
            stepProgress.CompletedActivityIds = completedList.ToArray(); // Assign back as Array

            // Check completion logic
            if (CheckIfStepIsComplete(step, completedList))
            {
                stepProgress.Status = StepCompletionStatus.Completed;
                stepProgress.CompletedAt = DateTimeOffset.UtcNow;

                // Award XP
                attempt.TotalExperienceEarned += step.ExperiencePoints;
                await _attemptRepository.UpdateAsync(attempt, cancellationToken);
            }
            else
            {
                stepProgress.Status = StepCompletionStatus.InProgress;
            }

            stepProgress.UpdatedAt = DateTimeOffset.UtcNow;
            await _stepProgressRepository.UpdateAsync(stepProgress, cancellationToken);
        }
        else if (request.Status != StepCompletionStatus.Completed && isAlreadyCompleted)
        {
            // Revert logic
            completedList.Remove(request.ActivityId);
            stepProgress.CompletedActivityIds = completedList.ToArray(); // Assign back as Array

            stepProgress.Status = StepCompletionStatus.InProgress;
            stepProgress.UpdatedAt = DateTimeOffset.UtcNow;
            await _stepProgressRepository.UpdateAsync(stepProgress, cancellationToken);
        }
    }

    private bool CheckIfStepIsComplete(QuestStep step, List<Guid> completedActivityIds)
    {
        try
        {
            if (step.Content == null) return true;

            // Safe deserialization of the Content object (JSONB)
            var jsonString = JsonSerializer.Serialize(step.Content);
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            if (root.TryGetProperty("activities", out var activitiesElement) && activitiesElement.ValueKind == JsonValueKind.Array)
            {
                int totalActivities = activitiesElement.GetArrayLength();
                int matchedCount = 0;

                foreach (var activity in activitiesElement.EnumerateArray())
                {
                    // Extract activityId from JSON and parse to Guid
                    if (activity.TryGetProperty("activityId", out var idElement) &&
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
        catch
        {
            // Fallback: If we can't parse structure, assume incomplete to be safe
            return false;
        }

        return false;
    }
}