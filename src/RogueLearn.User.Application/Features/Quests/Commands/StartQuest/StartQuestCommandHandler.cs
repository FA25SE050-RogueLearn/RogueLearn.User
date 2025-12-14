// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/StartQuest/StartQuestCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Services; // Added for IQuestDifficultyResolver
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Quests.Commands.StartQuest;

public class StartQuestCommandHandler : IRequestHandler<StartQuestCommand, StartQuestResponse>
{
    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly IQuestRepository _questRepository;
    // Added dependencies for JIT difficulty calculation
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
        _logger.LogInformation("Starting quest {QuestId} for user {UserId}", request.QuestId, request.AuthUserId);

        // 1. Check if quest exists
        var quest = await _questRepository.GetByIdAsync(request.QuestId, cancellationToken);
        if (quest == null)
        {
            throw new NotFoundException("Quest", request.QuestId);
        }

        // 2. Check if attempt already exists (Idempotency)
        var existingAttempt = await _attemptRepository.FirstOrDefaultAsync(
            a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
            cancellationToken);

        if (existingAttempt != null)
        {
            _logger.LogInformation("User {UserId} already has an attempt for quest {QuestId}. Returning existing.", request.AuthUserId, request.QuestId);
            return new StartQuestResponse
            {
                AttemptId = existingAttempt.Id,
                Status = existingAttempt.Status.ToString(),
                AssignedDifficulty = existingAttempt.AssignedDifficulty,
                IsNew = false
            };
        }

        // 3. Calculate Personalized Difficulty (Just-In-Time)
        string assignedDifficulty = "Standard";
        string? difficultyReason = null;

        if (quest.SubjectId.HasValue)
        {
            // Fetch the user's specific grade record for this subject using the helper method
            // that handles the string-based Guid conversion internally
            var gradeRecords = await _studentSubjectRepository.GetSemesterSubjectsByUserAsync(request.AuthUserId, cancellationToken);
            var subjectRecord = gradeRecords.FirstOrDefault(s => s.SubjectId == quest.SubjectId.Value);

            // Use the resolver logic to determine difficulty (Challenging/Standard/Supportive)
            var difficultyInfo = _difficultyResolver.ResolveDifficulty(subjectRecord);
            assignedDifficulty = difficultyInfo.ExpectedDifficulty;
            difficultyReason = difficultyInfo.DifficultyReason;
        }
        else
        {
            // Fallback for non-subject quests
            assignedDifficulty = !string.IsNullOrEmpty(quest.ExpectedDifficulty) ? quest.ExpectedDifficulty : "Standard";
        }

        // 4. Create new attempt
        var newAttempt = new UserQuestAttempt
        {
            Id = Guid.NewGuid(),
            AuthUserId = request.AuthUserId,
            QuestId = request.QuestId,
            Status = QuestAttemptStatus.InProgress,
            AssignedDifficulty = assignedDifficulty,
            Notes = difficultyReason,
            StartedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var createdAttempt = await _attemptRepository.AddAsync(newAttempt, cancellationToken);

        _logger.LogInformation("Successfully created attempt {AttemptId} for quest {QuestId} with JIT calculated difficulty: {Difficulty}",
            createdAttempt.Id, request.QuestId, createdAttempt.AssignedDifficulty);

        return new StartQuestResponse
        {
            AttemptId = createdAttempt.Id,
            Status = createdAttempt.Status.ToString(),
            AssignedDifficulty = createdAttempt.AssignedDifficulty,
            IsNew = true
        };
    }
}