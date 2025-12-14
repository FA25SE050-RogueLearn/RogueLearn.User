// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/GenerateQuestLine/GenerateQuestLineCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum;

public class GenerateQuestLineCommandHandler : IRequestHandler<GenerateQuestLine, GenerateQuestLineResponse>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IClassSpecializationSubjectRepository _classSpecializationSubjectRepository;
    private readonly IStudentSemesterSubjectRepository _studentSemesterSubjectRepository;
    // REMOVED: ILearningPathRepository dependency (No DB table used)
    private readonly IQuestRepository _questRepository;
    private readonly IUserQuestAttemptRepository _userQuestAttemptRepository;
    private readonly IQuestDifficultyResolver _difficultyResolver;
    private readonly ILogger<GenerateQuestLineCommandHandler> _logger;

    public GenerateQuestLineCommandHandler(
        IUserProfileRepository userProfileRepository,
        ISubjectRepository subjectRepository,
        IClassSpecializationSubjectRepository classSpecializationSubjectRepository,
        IStudentSemesterSubjectRepository studentSemesterSubjectRepository,
        // REMOVED: ILearningPathRepository
        IQuestRepository questRepository,
        IUserQuestAttemptRepository userQuestAttemptRepository,
        IQuestDifficultyResolver difficultyResolver,
        ILogger<GenerateQuestLineCommandHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _subjectRepository = subjectRepository;
        _classSpecializationSubjectRepository = classSpecializationSubjectRepository;
        _studentSemesterSubjectRepository = studentSemesterSubjectRepository;
        _questRepository = questRepository;
        _userQuestAttemptRepository = userQuestAttemptRepository;
        _difficultyResolver = difficultyResolver;
        _logger = logger;
    }

    public async Task<GenerateQuestLineResponse> Handle(GenerateQuestLine request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating quest line for user {AuthUserId}", request.AuthUserId);

        // 1. Validation & Profile Loading
        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken);
        if (userProfile is null) throw new BadRequestException("Invalid User Profile");
        if (userProfile.RouteId is null || userProfile.ClassId is null) throw new BadRequestException("Please select a route and class first.");

        // NOTE: We no longer create a physical "LearningPath" record in the database.
        // The path is now a virtual concept derived from the user's Route and Class.
        // We will return the AuthUserId as the LearningPathId to satisfy the API contract.

        // 2. Identify Target Subjects (From Route + Class)
        var routeSubjects = await _subjectRepository.GetSubjectsByRoute(userProfile.RouteId.Value, cancellationToken);
        var classSubjects = await _classSpecializationSubjectRepository.GetSubjectByClassIdAsync(userProfile.ClassId.Value, cancellationToken);

        var idealSubjects = routeSubjects.Concat(classSubjects)
            .DistinctBy(s => s.Id)
            .ToList();

        _logger.LogInformation("Found {Count} total subjects for user's academic path.", idealSubjects.Count);

        // 3. Get User's Academic History (Grades) for Difficulty Preview
        var gradeRecords = (await _studentSemesterSubjectRepository.GetSemesterSubjectsByUserAsync(request.AuthUserId, cancellationToken)).ToList();

        // 4. The Core Loop: Link Subjects -> Quests & Assign Difficulty (Gap Analysis)
        foreach (var subject in idealSubjects)
        {
            // A. Find the Master Quest for this subject
            var masterQuest = await _questRepository.GetActiveQuestBySubjectIdAsync(subject.Id, cancellationToken);

            // If admin hasn't generated a quest for this subject yet, skip it.
            if (masterQuest == null)
            {
                continue;
            }

            // B. Calculate Personalized Difficulty (The Gap Analysis Logic)
            var gradeRecord = gradeRecords.FirstOrDefault(g => g.SubjectId == subject.Id);
            var difficultyInfo = _difficultyResolver.ResolveDifficulty(gradeRecord);

            // C. Create or Update the User's Attempt (Status: NotStarted)
            var existingAttempt = await _userQuestAttemptRepository.FirstOrDefaultAsync(
                a => a.AuthUserId == request.AuthUserId && a.QuestId == masterQuest.Id,
                cancellationToken);

            if (existingAttempt == null)
            {
                // Create new attempt 
                // MODIFIED: 'AssignedDifficulty' removed. Difficulty is now resolved dynamically at read-time.
                var newAttempt = new UserQuestAttempt
                {
                    AuthUserId = request.AuthUserId,
                    QuestId = masterQuest.Id,
                    // IMPORTANT: Set status to NotStarted. This ensures it shows up in "My Quests" lists
                    // but the timer doesn't start yet.
                    Status = QuestAttemptStatus.NotStarted,
                    Notes = difficultyInfo.DifficultyReason, // Keep reason for history/debug
                    StartedAt = DateTimeOffset.UtcNow
                };
                await _userQuestAttemptRepository.AddAsync(newAttempt, cancellationToken);
                _logger.LogInformation("Generated NotStarted attempt for Quest {QuestId}.", masterQuest.Id);
            }
            else
            {
                // If exists but hasn't really started, we can update notes if needed, but logic is lighter now.
                if (existingAttempt.Status == QuestAttemptStatus.NotStarted)
                {
                    existingAttempt.Notes = $"Difficulty updated (Preview): {difficultyInfo.DifficultyReason}";
                    await _userQuestAttemptRepository.UpdateAsync(existingAttempt, cancellationToken);
                }
            }
        }

        // Return AuthUserId as the virtual LearningPathId
        return new GenerateQuestLineResponse { LearningPathId = request.AuthUserId };
    }
}