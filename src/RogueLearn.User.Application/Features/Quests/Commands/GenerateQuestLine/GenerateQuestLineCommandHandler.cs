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
    private readonly ILearningPathRepository _learningPathRepository;
    private readonly IQuestChapterRepository _questChapterRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IUserQuestAttemptRepository _userQuestAttemptRepository; // NEW
    private readonly IQuestDifficultyResolver _difficultyResolver;
    private readonly ILogger<GenerateQuestLineCommandHandler> _logger;

    public GenerateQuestLineCommandHandler(
        IUserProfileRepository userProfileRepository,
        ISubjectRepository subjectRepository,
        IClassSpecializationSubjectRepository classSpecializationSubjectRepository,
        IStudentSemesterSubjectRepository studentSemesterSubjectRepository,
        ILearningPathRepository learningPathRepository,
        IQuestChapterRepository questChapterRepository,
        IQuestRepository questRepository,
        IUserQuestAttemptRepository userQuestAttemptRepository,
        IQuestDifficultyResolver difficultyResolver,
        ILogger<GenerateQuestLineCommandHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _subjectRepository = subjectRepository;
        _classSpecializationSubjectRepository = classSpecializationSubjectRepository;
        _studentSemesterSubjectRepository = studentSemesterSubjectRepository;
        _learningPathRepository = learningPathRepository;
        _questChapterRepository = questChapterRepository;
        _questRepository = questRepository;
        _userQuestAttemptRepository = userQuestAttemptRepository;
        _difficultyResolver = difficultyResolver;
        _logger = logger;
    }

    public async Task<GenerateQuestLineResponse> Handle(GenerateQuestLine request, CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId);
        if (userProfile is null) throw new BadRequestException("Invalid User Profile");
        if (userProfile.RouteId is null || userProfile.ClassId is null) throw new BadRequestException("Please select a route and class first.");

        // 1. Get or Create Personal Learning Path (This remains personal as it holds the user's chapter progress structure)
        var learningPath = await _learningPathRepository.GetLatestByUserAsync(request.AuthUserId, cancellationToken);
        if (learningPath == null)
        {
            learningPath = new LearningPath
            {
                Name = $"{userProfile.Username}'s Path",
                PathType = PathType.Course,
                IsPublished = false,
                CreatedBy = userProfile.AuthUserId
            };
            learningPath = await _learningPathRepository.AddAsync(learningPath, cancellationToken);
        }

        // 2. Identify Target Subjects (Same as before)
        var routeSubjects = await _subjectRepository.GetSubjectsByRoute(userProfile.RouteId.Value, cancellationToken);
        var classSubjects = await _classSpecializationSubjectRepository.GetSubjectByClassIdAsync(userProfile.ClassId.Value, cancellationToken);
        var idealSubjects = routeSubjects.Concat(classSubjects).DistinctBy(s => s.Id).ToList();

        // 3. Get User's Academic History (Grades)
        var userGrades = await _studentSemesterSubjectRepository.GetSubjectsByUserAsync(request.AuthUserId, cancellationToken);
        // Need to fetch actual grade records to get the scores
        var gradeRecords = (await _studentSemesterSubjectRepository.FindAsync(ss => ss.AuthUserId == request.AuthUserId, cancellationToken)).ToList();

        // 4. Process Each Subject -> Link to Master Quest
        foreach (var subject in idealSubjects)
        {
            // A. Find the Master Quest for this subject (Created by Admin/System)
            // We assume there is only ONE active quest per subject in the system.
            var masterQuest = (await _questRepository.FindAsync(
                q => q.SubjectId == subject.Id && q.IsActive,
                cancellationToken
            )).FirstOrDefault();

            if (masterQuest == null)
            {
                _logger.LogWarning("No Master Quest found for subject {SubjectCode}. Skipping.", subject.SubjectCode);
                continue;
                // In a real scenario, we might flag this for Admin to generate content.
            }

            // B. Calculate Personalized Difficulty
            var gradeRecord = gradeRecords.FirstOrDefault(g => g.SubjectId == subject.Id);
            var difficultyInfo = _difficultyResolver.ResolveDifficulty(gradeRecord);

            // C. Create or Update the User's Attempt (The Link)
            var existingAttempt = await _userQuestAttemptRepository.FirstOrDefaultAsync(
                a => a.AuthUserId == request.AuthUserId && a.QuestId == masterQuest.Id,
                cancellationToken);

            if (existingAttempt == null)
            {
                var newAttempt = new UserQuestAttempt
                {
                    AuthUserId = request.AuthUserId,
                    QuestId = masterQuest.Id,
                    Status = QuestAttemptStatus.InProgress, // Or NotStarted, depending on logic

                    // ⭐ THIS IS THE KEY: Storing the personalization here, not on the Quest
                    AssignedDifficulty = difficultyInfo.ExpectedDifficulty,

                    Notes = difficultyInfo.DifficultyReason, // Store reason for transparency
                    StartedAt = DateTimeOffset.UtcNow
                };
                await _userQuestAttemptRepository.AddAsync(newAttempt, cancellationToken);
                _logger.LogInformation("Linked user to Master Quest {QuestId} with difficulty {Diff}", masterQuest.Id, difficultyInfo.ExpectedDifficulty);
            }
            else
            {
                // Optional: Update difficulty if academic record changed significantly?
                // Usually we keep it stable once started, but for "Sync", we might update it.
                if (existingAttempt.AssignedDifficulty != difficultyInfo.ExpectedDifficulty)
                {
                    existingAttempt.AssignedDifficulty = difficultyInfo.ExpectedDifficulty;
                    existingAttempt.Notes = $"Difficulty updated: {difficultyInfo.DifficultyReason}";
                    await _userQuestAttemptRepository.UpdateAsync(existingAttempt, cancellationToken);
                }
            }
        }

        return new GenerateQuestLineResponse { LearningPathId = learningPath.Id };
    }
}