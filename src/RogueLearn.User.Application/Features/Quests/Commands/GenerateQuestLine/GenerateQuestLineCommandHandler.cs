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
    private readonly IUserQuestAttemptRepository _userQuestAttemptRepository;
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
        _logger.LogInformation("Generating quest line for user {AuthUserId}", request.AuthUserId);

        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken);
        if (userProfile is null) throw new BadRequestException("Invalid User Profile");
        if (userProfile.RouteId is null || userProfile.ClassId is null) throw new BadRequestException("Please select a route and class first.");

        // 1. Get or Create Personal Learning Path
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

        // 2. Identify Target Subjects
        var routeSubjects = await _subjectRepository.GetSubjectsByRoute(userProfile.RouteId.Value, cancellationToken);
        var classSubjects = await _classSpecializationSubjectRepository.GetSubjectByClassIdAsync(userProfile.ClassId.Value, cancellationToken);

        var idealSubjects = routeSubjects.Concat(classSubjects)
            .DistinctBy(s => s.Id)
            .ToList();

        _logger.LogInformation("Found {Count} total subjects for user's academic path.", idealSubjects.Count);

        // 3. Get User's Academic History (Grades)
        // FIX: Replaced FindAsync with specialized method to avoid Guid LINQ issues
        var gradeRecords = (await _studentSemesterSubjectRepository.GetSemesterSubjectsByUserAsync(request.AuthUserId, cancellationToken)).ToList();

        // 4. Process Each Subject -> Link to Master Quest
        foreach (var subject in idealSubjects)
        {
            // A. Find the Master Quest for this subject (Created by Admin/System)
            // FIX: Replaced FindAsync with specialized method to avoid Guid LINQ issues (NullReferenceException in Supabase client)
            var masterQuest = await _questRepository.GetActiveQuestBySubjectIdAsync(subject.Id, cancellationToken);

            if (masterQuest == null)
            {
                // Silent skip - content might not be generated yet
                continue;
            }

            // B. Calculate Personalized Difficulty based on grades
            var gradeRecord = gradeRecords.FirstOrDefault(g => g.SubjectId == subject.Id);
            var difficultyInfo = _difficultyResolver.ResolveDifficulty(gradeRecord);

        }

     
        // Actually, to be 100% sure we fix the crash, let's look at the previous error:
        // It failed on `q => q.SubjectId == subject.Id`. SubjectId is nullable Guid?.
        // `UserQuestAttempt` keys are non-nullable Guids. It *should* be safer.

        foreach (var subject in idealSubjects)
        {
            var masterQuest = await _questRepository.GetActiveQuestBySubjectIdAsync(subject.Id, cancellationToken);
            if (masterQuest == null) continue;

            var gradeRecord = gradeRecords.FirstOrDefault(g => g.SubjectId == subject.Id);
            var difficultyInfo = _difficultyResolver.ResolveDifficulty(gradeRecord);

            // Fetch attempt safely (assuming Guid==Guid works better than Guid?==Guid)
            var existingAttempt = await _userQuestAttemptRepository.FirstOrDefaultAsync(
                a => a.AuthUserId == request.AuthUserId && a.QuestId == masterQuest.Id,
                cancellationToken);

            if (existingAttempt == null)
            {
                var newAttempt = new UserQuestAttempt
                {
                    AuthUserId = request.AuthUserId,
                    QuestId = masterQuest.Id,
                    Status = QuestAttemptStatus.InProgress,
                    AssignedDifficulty = difficultyInfo.ExpectedDifficulty,
                    Notes = difficultyInfo.DifficultyReason,
                    StartedAt = DateTimeOffset.UtcNow
                };
                await _userQuestAttemptRepository.AddAsync(newAttempt, cancellationToken);
                _logger.LogInformation("Linked user to Master Quest {QuestId} with difficulty {Diff}", masterQuest.Id, difficultyInfo.ExpectedDifficulty);
            }
            else
            {
                if (existingAttempt.AssignedDifficulty != difficultyInfo.ExpectedDifficulty)
                {
                    existingAttempt.AssignedDifficulty = difficultyInfo.ExpectedDifficulty;
                    existingAttempt.Notes = $"Difficulty updated: {difficultyInfo.DifficultyReason}";
                    await _userQuestAttemptRepository.UpdateAsync(existingAttempt, cancellationToken);
                    _logger.LogInformation("Updated difficulty for Quest {QuestId} to {Diff}", masterQuest.Id, difficultyInfo.ExpectedDifficulty);
                }
            }
        }

        return new GenerateQuestLineResponse { LearningPathId = learningPath.Id };
    }
}