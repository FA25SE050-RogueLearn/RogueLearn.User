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

        _logger.LogInformation("Generating QuestLine structure for User: {username}", userProfile.Username);

        var existingLearningPath = (await _learningPathRepository.FindAsync(lp => lp.CreatedBy == request.AuthUserId, cancellationToken))
            .FirstOrDefault();
        if (existingLearningPath != null)
        {
            _logger.LogInformation("Existing learning path {LearningPathId} found for user. Deleting to regenerate.", existingLearningPath.Id);

            var chaptersToDelete = (await _questChapterRepository.FindAsync(c => c.LearningPathId == existingLearningPath.Id, cancellationToken)).ToList();
            var chapterIds = chaptersToDelete.Select(c => c.Id).ToList();

            if (chapterIds.Any())
            {
                var questsToDelete = (await _questRepository.GetAllAsync(cancellationToken))
                    .Where(q => q.QuestChapterId.HasValue && chapterIds.Contains(q.QuestChapterId.Value))
                    .ToList();
                var questIdsToDelete = questsToDelete.Select(q => q.Id).ToList();

                if (questIdsToDelete.Any())
                {
                    _logger.LogInformation("Deleting {QuestCount} quests from old learning path.", questIdsToDelete.Count);
                    await _questRepository.DeleteRangeAsync(questIdsToDelete, cancellationToken);
                }
            }

            await _learningPathRepository.DeleteAsync(existingLearningPath.Id, cancellationToken);
        }

        var learningPathToCreate = new LearningPath
        {
            Name = $"{userProfile.Username}'s Path of Knowledge",
            Description = $"Your learning journey awaits!",
            PathType = PathType.Course,
            IsPublished = true,
            CreatedBy = userProfile.AuthUserId
        };

        var persistedLearningPath = await _learningPathRepository.AddAsync(learningPathToCreate, cancellationToken);

        var routeSubjects = await _subjectRepository.GetSubjectsByRoute(userProfile.RouteId.Value, cancellationToken);
        var classSubjects = await _classSpecializationSubjectRepository.GetSubjectByClassIdAsync(userProfile.ClassId.Value, cancellationToken);

        var allUserSubjects = routeSubjects.Concat(classSubjects).DistinctBy(s => s.Id).ToList();

        var semesterSubjectsMap = allUserSubjects
            .Where(subject => subject.Semester.HasValue)
            .GroupBy(subject => subject.Semester!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.ToList()
            );

        var questChaptersToInsert = new List<QuestChapter>();
        var chapterPreperationList = new List<(QuestChapter chapter, List<Subject> subjects)>();

        foreach (var (semesterNumber, subjectsInSemester) in semesterSubjectsMap.OrderBy(kvp => kvp.Key))
        {
            var questChapter = new QuestChapter
            {
                Id = Guid.NewGuid(),
                LearningPathId = persistedLearningPath.Id,
                Title = $"Semester {semesterNumber}",
                Sequence = semesterNumber,
                Status = PathProgressStatus.NotStarted,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            questChaptersToInsert.Add(questChapter);
            chapterPreperationList.Add((questChapter, subjectsInSemester));
        }

        var persistedChapters = (await _questChapterRepository.AddRangeAsync(questChaptersToInsert, cancellationToken)).ToList();

        var questsToInsert = new List<Quest>();

        foreach (var persistedChapter in persistedChapters)
        {
            var originalChapterData = chapterPreperationList.FirstOrDefault(p => p.chapter.Title == persistedChapter.Title);
            if (originalChapterData.subjects == null) continue;

            int questSequence = 1;
            foreach (var subjectDetails in originalChapterData.subjects.OrderBy(s => s.SubjectCode))
            {
                // ARCHITECTURAL FIX: The content check is removed from here.
                // This handler's responsibility is to create the quest 'shell' for ALL subjects in the curriculum.
                // The check for content readiness will be performed by the process that schedules the step generation.

                var quest = new Quest
                {
                    Id = Guid.NewGuid(),
                    Title = subjectDetails.SubjectName,
                    Description = subjectDetails.Description ?? $"Embark on the quest for {subjectDetails.SubjectName}.",
                    QuestType = QuestType.Practice,
                    DifficultyLevel = DifficultyLevel.Beginner,
                    QuestChapterId = persistedChapter.Id,
                    SubjectId = subjectDetails.Id,
                    Sequence = questSequence++,
                    Status = QuestStatus.NotStarted,
                    CreatedBy = userProfile.AuthUserId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                questsToInsert.Add(quest);
            }
        }

        await _questRepository.AddRangeAsync(questsToInsert, cancellationToken);
        _logger.LogInformation("Successfully persisted {QuestCount} quest shells to the database.", questsToInsert.Count);

        _logger.LogInformation("Successfully generated QuestLine structure {LearningPathId}", persistedLearningPath.Id);

        return new GenerateQuestLineResponse
        {
            LearningPathId = persistedLearningPath.Id,
        };
    }
}