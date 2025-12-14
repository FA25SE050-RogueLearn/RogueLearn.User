// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Queries/GetQuestProgress/GetQuestProgressQueryHandler.cs
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Services; // Needed for IQuestDifficultyResolver
using System.Text.Json;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetQuestProgress;

public class GetQuestProgressQueryHandler : IRequestHandler<GetQuestProgressQuery, List<QuestStepProgressDto>>
{
    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IQuestStepRepository _stepRepository;
    private readonly IUserQuestStepProgressRepository _progressRepository;
    // MODIFIED: Added dependencies for dynamic difficulty calculation
    private readonly IStudentSemesterSubjectRepository _studentSubjectRepository;
    private readonly IQuestDifficultyResolver _difficultyResolver;

    public GetQuestProgressQueryHandler(
        IUserQuestAttemptRepository attemptRepository,
        IQuestRepository questRepository,
        IQuestStepRepository stepRepository,
        IUserQuestStepProgressRepository progressRepository,
        IStudentSemesterSubjectRepository studentSubjectRepository,
        IQuestDifficultyResolver difficultyResolver)
    {
        _attemptRepository = attemptRepository;
        _questRepository = questRepository;
        _stepRepository = stepRepository;
        _progressRepository = progressRepository;
        _studentSubjectRepository = studentSubjectRepository;
        _difficultyResolver = difficultyResolver;
    }

    public async Task<List<QuestStepProgressDto>> Handle(GetQuestProgressQuery request, CancellationToken cancellationToken)
    {
        // 1. Get the User's Attempt
        var attempt = await _attemptRepository.FirstOrDefaultAsync(
            a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
            cancellationToken);

        if (attempt == null)
        {
            throw new NotFoundException("Quest not started. No progress available.");
        }

        // 2. Resolve Difficulty Dynamically
        // MODIFIED: Changed from attempt.AssignedDifficulty to dynamic resolution
        string assignedDifficulty = "Standard";
        var quest = await _questRepository.GetByIdAsync(request.QuestId, cancellationToken);

        if (quest != null && quest.SubjectId.HasValue)
        {
            var gradeRecords = await _studentSubjectRepository.GetSemesterSubjectsByUserAsync(request.AuthUserId, cancellationToken);
            var subjectRecord = gradeRecords.FirstOrDefault(s => s.SubjectId == quest.SubjectId.Value);

            var difficultyInfo = _difficultyResolver.ResolveDifficulty(subjectRecord);
            assignedDifficulty = difficultyInfo.ExpectedDifficulty;
        }

        // 3. Get All Master Steps
        var allSteps = await _stepRepository.GetByQuestIdAsync(request.QuestId, cancellationToken);

        // 4. Filter Steps by Calculated Difficulty
        var userTrackSteps = allSteps
            .Where(s => string.Equals(s.DifficultyVariant, assignedDifficulty, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.StepNumber)
            .ToList();

        if (!userTrackSteps.Any())
        {
            return new List<QuestStepProgressDto>();
        }

        // 5. Get Existing Progress Records
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