// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/GenerateQuestLine/GenerateQuestLineCommandHandler.cs
using Hangfire;
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
    private readonly ILogger<GenerateQuestLineCommandHandler> _logger;

    public GenerateQuestLineCommandHandler(
        IUserProfileRepository userProfileRepository,
        ISubjectRepository subjectRepository,
        IClassSpecializationSubjectRepository classSpecializationSubjectRepository,
        IStudentSemesterSubjectRepository studentSemesterSubjectRepository,
        ILearningPathRepository learningPathRepository,
        IQuestChapterRepository questChapterRepository,
        IQuestRepository questRepository,
        ILogger<GenerateQuestLineCommandHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _subjectRepository = subjectRepository;
        _classSpecializationSubjectRepository = classSpecializationSubjectRepository;
        _studentSemesterSubjectRepository = studentSemesterSubjectRepository;
        _learningPathRepository = learningPathRepository;
        _questChapterRepository = questChapterRepository;
        _questRepository = questRepository;
        _logger = logger;
    }

    public async Task<GenerateQuestLineResponse> Handle(GenerateQuestLine request, CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId);
        if (userProfile is null)
        {
            throw new BadRequestException("Invalid User Profile");
        }

        if (userProfile.RouteId is null)
        {
            throw new BadRequestException("Please select a route first.");
        }

        if (userProfile.ClassId is null)
        {
            throw new BadRequestException("Please select a class first.");
        }

        _logger.LogInformation("Reconciling QuestLine structure for User: {username}", userProfile.Username);

        // ARCHITECTURAL CHANGE: Fetch existing learning path or create a new one. DO NOT DELETE.
        var learningPath = (await _learningPathRepository.FindAsync(lp => lp.CreatedBy == request.AuthUserId, cancellationToken))
            .FirstOrDefault();

        if (learningPath == null)
        {
            _logger.LogInformation("No existing learning path found. Creating a new one for user {AuthUserId}", request.AuthUserId);
            learningPath = new LearningPath
            {
                Name = $"{userProfile.Username}'s Path of Knowledge",
                Description = $"Your learning journey awaits!",
                PathType = PathType.Course,
                IsPublished = true,
                CreatedBy = userProfile.AuthUserId
            };
            learningPath = await _learningPathRepository.AddAsync(learningPath, cancellationToken);
        }

        // --- Start Reconciliation Logic ---

        // 1. Get the "ideal" state from the curriculum
        var routeSubjects = await _subjectRepository.GetSubjectsByRoute(userProfile.RouteId.Value, cancellationToken);
        var classSubjects = await _classSpecializationSubjectRepository.GetSubjectByClassIdAsync(userProfile.ClassId.Value, cancellationToken);
        var idealSubjects = routeSubjects.Concat(classSubjects).DistinctBy(s => s.Id).ToList();
        var idealSubjectIds = idealSubjects.Select(s => s.Id).ToHashSet();

        // 2. Get the "current" state from the user's learning path
        var currentChapters = (await _questChapterRepository.FindAsync(c => c.LearningPathId == learningPath.Id, cancellationToken)).ToList();
        var currentQuests = (await _questRepository.GetAllAsync(cancellationToken))
            .Where(q => q.QuestChapterId.HasValue && currentChapters.Select(c => c.Id).Contains(q.QuestChapterId.Value))
            .ToList();
        var currentSubjectIdsWithQuests = currentQuests.Where(q => q.SubjectId.HasValue).Select(q => q.SubjectId!.Value).ToHashSet();

        // 3. Identify quests to archive (subjects that are no longer in the curriculum)
        var questsToArchive = currentQuests
            .Where(q => q.SubjectId.HasValue && !idealSubjectIds.Contains(q.SubjectId.Value) && q.IsActive)
            .ToList();

        if (questsToArchive.Any())
        {
            _logger.LogInformation("Archiving {Count} quests for subjects no longer in the user's curriculum.", questsToArchive.Count);
            foreach (var quest in questsToArchive)
            {
                quest.IsActive = false; // Soft delete
                await _questRepository.UpdateAsync(quest, cancellationToken);
            }
        }

        // 4. Group ideal subjects by semester to create/update chapters and quests
        var idealSemesterMap = idealSubjects
            .Where(subject => subject.Semester.HasValue)
            .GroupBy(subject => subject.Semester!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.ToList()
            );

        foreach (var (semesterNumber, subjectsInSemester) in idealSemesterMap.OrderBy(kvp => kvp.Key))
        {
            // Find or create the chapter for this semester
            var chapterTitle = $"Semester {semesterNumber}";
            var chapter = currentChapters.FirstOrDefault(c => c.Title == chapterTitle);
            if (chapter == null)
            {
                chapter = new QuestChapter
                {
                    LearningPathId = learningPath.Id,
                    Title = chapterTitle,
                    Sequence = semesterNumber,
                    Status = PathProgressStatus.NotStarted
                };
                chapter = await _questChapterRepository.AddAsync(chapter, cancellationToken);
                _logger.LogInformation("Created new QuestChapter: {Title}", chapter.Title);
            }

            // 5. Identify new quests to create for this chapter
            int questSequence = 1;
            foreach (var subject in subjectsInSemester.OrderBy(s => s.SubjectCode))
            {
                if (!currentSubjectIdsWithQuests.Contains(subject.Id))
                {
                    // This subject is in the ideal curriculum but doesn't have a quest yet. Create one.
                    _logger.LogInformation("Creating new shell quest for Subject {SubjectCode}", subject.SubjectCode);
                    var newQuest = new Quest
                    {
                        Title = subject.SubjectName,
                        Description = subject.Description ?? $"Embark on the quest for {subject.SubjectName}.",
                        QuestType = QuestType.Practice,
                        DifficultyLevel = DifficultyLevel.Beginner,
                        QuestChapterId = chapter.Id,
                        SubjectId = subject.Id,
                        Sequence = questSequence++,
                        Status = QuestStatus.NotStarted,
                        IsActive = true, // New quests are active by default
                        CreatedBy = userProfile.AuthUserId
                    };
                    await _questRepository.AddAsync(newQuest, cancellationToken);
                }
                else
                {
                    // If the quest already exists, we could update its details here if needed (e.g., name change).
                    // For now, we just ensure its sequence is correct.
                    var existingQuest = currentQuests.First(q => q.SubjectId == subject.Id);
                    if (existingQuest.Sequence != questSequence || !existingQuest.IsActive)
                    {
                        existingQuest.Sequence = questSequence;
                        existingQuest.IsActive = true; // Re-activate if it was previously archived
                        await _questRepository.UpdateAsync(existingQuest, cancellationToken);
                    }
                    questSequence++;
                }
            }
        }

        _logger.LogInformation("Successfully reconciled QuestLine structure {LearningPathId}", learningPath.Id);

        return new GenerateQuestLineResponse
        {
            LearningPathId = learningPath.Id,
        };
    }
}