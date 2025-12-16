// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/StartQuest/StartQuestCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Quests.Commands.StartQuest;

public class StartQuestCommandHandler : IRequestHandler<StartQuestCommand, StartQuestResponse>
{
    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IStudentSemesterSubjectRepository _studentSubjectRepository;
    private readonly IQuestDifficultyResolver _difficultyResolver;
    private readonly ILogger<StartQuestCommandHandler> _logger;

    public StartQuestCommandHandler(
        IUserQuestAttemptRepository attemptRepository,
        IQuestRepository questRepository,
        IStudentSemesterSubjectRepository studentSubjectRepository,
        IQuestDifficultyResolver difficultyResolver,
        ILogger<StartQuestCommandHandler> logger)
    {
        _attemptRepository = attemptRepository;
        _questRepository = questRepository;
        _studentSubjectRepository = studentSubjectRepository;
        _difficultyResolver = difficultyResolver;
        _logger = logger;
    }

    public async Task<StartQuestResponse> Handle(StartQuestCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("StartQuest: User {UserId} requesting to start quest {QuestId}", request.AuthUserId, request.QuestId);

        // 1. Check if quest exists
        var quest = await _questRepository.GetByIdAsync(request.QuestId, cancellationToken);
        if (quest == null)
        {
            throw new NotFoundException("Quest", request.QuestId);
        }

        // 2. Check for existing attempt
        var existingAttempt = await _attemptRepository.FirstOrDefaultAsync(
            a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
            cancellationToken);

        // 3. Resolve Difficulty (Current Calculation)
        string calculatedDifficulty = "Standard";
        if (quest.SubjectId.HasValue)
        {
            var gradeRecords = await _studentSubjectRepository.GetSemesterSubjectsByUserAsync(request.AuthUserId, cancellationToken);
            var subjectRecord = gradeRecords.FirstOrDefault(s => s.SubjectId == quest.SubjectId.Value);

            var difficultyInfo = _difficultyResolver.ResolveDifficulty(subjectRecord);
            calculatedDifficulty = difficultyInfo.ExpectedDifficulty;
        }
        else if (!string.IsNullOrEmpty(quest.ExpectedDifficulty))
        {
            calculatedDifficulty = quest.ExpectedDifficulty;
        }

        // 4. Handle State Transitions
        if (existingAttempt != null)
        {
            // CASE A: Already Active/Completed
            // We respect the HISTORICAL difficulty locked in the attempt, ignoring the new calculation.
            if (existingAttempt.Status != QuestAttemptStatus.NotStarted)
            {
                _logger.LogInformation("Quest {QuestId} already active. Maintaining locked difficulty: {Difficulty}",
                    request.QuestId, existingAttempt.AssignedDifficulty);

                return new StartQuestResponse
                {
                    AttemptId = existingAttempt.Id,
                    Status = existingAttempt.Status.ToString(),
                    AssignedDifficulty = existingAttempt.AssignedDifficulty, // Return the snapshot
                    IsNew = false
                };
            }

            // CASE B: Transitioning from NotStarted -> InProgress (First Activation)
            // We LOCK IN the currently calculated difficulty now.
            _logger.LogInformation("Activating NotStarted attempt. Locking difficulty to: {Difficulty}", calculatedDifficulty);

            existingAttempt.Status = QuestAttemptStatus.InProgress;
            existingAttempt.AssignedDifficulty = calculatedDifficulty; // SNAPSHOT HAPPENS HERE
            existingAttempt.Notes = $"Started on {DateTime.UtcNow}. Difficulty locked.";
            existingAttempt.StartedAt = DateTimeOffset.UtcNow;
            existingAttempt.UpdatedAt = DateTimeOffset.UtcNow;

            await _attemptRepository.UpdateAsync(existingAttempt, cancellationToken);

            return new StartQuestResponse
            {
                AttemptId = existingAttempt.Id,
                Status = existingAttempt.Status.ToString(),
                AssignedDifficulty = calculatedDifficulty,
                IsNew = true
            };
        }

        // CASE C: No attempt exists (Fresh Start)
        // Create new attempt and LOCK IN the difficulty.
        var newAttempt = new UserQuestAttempt
        {
            Id = Guid.NewGuid(),
            AuthUserId = request.AuthUserId,
            QuestId = request.QuestId,
            Status = QuestAttemptStatus.InProgress,
            AssignedDifficulty = calculatedDifficulty, // SNAPSHOT HAPPENS HERE
            Notes = "First attempt",
            StartedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var createdAttempt = await _attemptRepository.AddAsync(newAttempt, cancellationToken);

        _logger.LogInformation("Created fresh attempt for Quest {QuestId} with locked difficulty {Difficulty}",
            createdAttempt.Id, calculatedDifficulty);

        return new StartQuestResponse
        {
            AttemptId = createdAttempt.Id,
            Status = createdAttempt.Status.ToString(),
            AssignedDifficulty = calculatedDifficulty,
            IsNew = true
        };
    }
}