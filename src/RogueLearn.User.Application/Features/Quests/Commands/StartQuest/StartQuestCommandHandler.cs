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

        // 3. Just-In-Time Difficulty Calculation logic (for return value only)
        // MODIFIED: We calculate this to return it to the UI, but we NO LONGER persist it to the attempt.
        string finalDifficulty = "Standard";

        if (quest.SubjectId.HasValue)
        {
            var gradeRecords = await _studentSubjectRepository.GetSemesterSubjectsByUserAsync(request.AuthUserId, cancellationToken);
            var subjectRecord = gradeRecords.FirstOrDefault(s => s.SubjectId == quest.SubjectId.Value);

            var difficultyInfo = _difficultyResolver.ResolveDifficulty(subjectRecord);
            finalDifficulty = difficultyInfo.ExpectedDifficulty;
        }
        else
        {
            finalDifficulty = !string.IsNullOrEmpty(quest.ExpectedDifficulty) ? quest.ExpectedDifficulty : "Standard";
        }


        // 4. Handle State Transitions
        if (existingAttempt != null)
        {
            // CASE A: Already Active/Completed
            if (existingAttempt.Status != QuestAttemptStatus.NotStarted)
            {
                _logger.LogInformation("Quest {QuestId} already active/completed for user. Returning existing status.", request.QuestId);
                return new StartQuestResponse
                {
                    AttemptId = existingAttempt.Id,
                    Status = existingAttempt.Status.ToString(),
                    AssignedDifficulty = finalDifficulty, // Dynamic
                    IsNew = false
                };
            }

            // CASE B: Transitioning from NotStarted -> InProgress (Activation)
            _logger.LogInformation("Activating NotStarted attempt for Quest {QuestId}.", request.QuestId);

            existingAttempt.Status = QuestAttemptStatus.InProgress;
            // MODIFIED: Removed assignment of AssignedDifficulty
            existingAttempt.Notes = $"Started with calculated difficulty: {finalDifficulty}";
            existingAttempt.StartedAt = DateTimeOffset.UtcNow; // Reset start time to actual interaction
            existingAttempt.UpdatedAt = DateTimeOffset.UtcNow;

            await _attemptRepository.UpdateAsync(existingAttempt, cancellationToken);

            return new StartQuestResponse
            {
                AttemptId = existingAttempt.Id,
                Status = existingAttempt.Status.ToString(),
                AssignedDifficulty = finalDifficulty,
                IsNew = true // Treat as new from UX perspective (it's "Fresh")
            };
        }

        // CASE C: No attempt exists (Fallback/Safety)
        var newAttempt = new UserQuestAttempt
        {
            Id = Guid.NewGuid(),
            AuthUserId = request.AuthUserId,
            QuestId = request.QuestId,
            Status = QuestAttemptStatus.InProgress,
            // MODIFIED: Removed assignment of AssignedDifficulty
            Notes = $"Started with calculated difficulty: {finalDifficulty}",
            StartedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var createdAttempt = await _attemptRepository.AddAsync(newAttempt, cancellationToken);

        _logger.LogInformation("Created fresh attempt for Quest {QuestId}", createdAttempt.Id);

        return new StartQuestResponse
        {
            AttemptId = createdAttempt.Id,
            Status = createdAttempt.Status.ToString(),
            AssignedDifficulty = finalDifficulty,
            IsNew = true
        };
    }
}