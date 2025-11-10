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
    private readonly IMediator _mediator;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public GenerateQuestLineCommandHandler(
        IUserProfileRepository userProfileRepository,
        ISubjectRepository subjectRepository,
        IClassSpecializationSubjectRepository classSpecializationSubjectRepository,
        IStudentSemesterSubjectRepository studentSemesterSubjectRepository,
        ILearningPathRepository learningPathRepository,
        IQuestChapterRepository questChapterRepository,
        IQuestRepository questRepository,
        ILogger<GenerateQuestLineCommandHandler> logger,
        IMediator mediator,
        IBackgroundJobClient backgroundJobClient
        )
    {
        _userProfileRepository = userProfileRepository;
        _subjectRepository = subjectRepository;
        _classSpecializationSubjectRepository = classSpecializationSubjectRepository;
        _studentSemesterSubjectRepository = studentSemesterSubjectRepository;
        _learningPathRepository = learningPathRepository;
        _questChapterRepository = questChapterRepository;
        _questRepository = questRepository;
        _logger = logger;
        _mediator = mediator;
        _backgroundJobClient = backgroundJobClient;
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

        _logger.LogInformation("Generating QuestLine for User: {username}", userProfile.Username);

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

        var subjectMap = routeSubjects.ToDictionary(
            subject => subject.Id,
            subject => false
        );

        var classSubjects = await _classSpecializationSubjectRepository.GetSubjectByClassIdAsync(userProfile.ClassId.Value, cancellationToken);

        foreach (var subject in classSubjects)
        {
            subjectMap[subject.Id] = false;
        }

        var userSubjects =
            await _studentSemesterSubjectRepository.FindAsync(s =>
                s.AuthUserId == userProfile.AuthUserId,
                cancellationToken
            );

        foreach (var subject in userSubjects)
        {
            if (subject.Status == SubjectEnrollmentStatus.Studying ||
                subject.Status == SubjectEnrollmentStatus.NotPassed)
            {
                subjectMap.TryAdd(subject.SubjectId, true);
            }
        }

        var semesterSubjectsMap = routeSubjects
            .Where(subject => subject.Semester.HasValue)
            .GroupBy(subject => subject.Semester!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.Select(subject => subject.Id).ToList()
            );

        foreach (var subject in classSubjects)
        {
            if (subject.Semester.HasValue)
            {
                var semesterKey = subject.Semester.Value;
                if (!semesterSubjectsMap.ContainsKey(semesterKey))
                {
                    semesterSubjectsMap[semesterKey] = new List<Guid>();
                }

                if (!semesterSubjectsMap[semesterKey].Contains(subject.Id))
                {
                    semesterSubjectsMap[semesterKey].Add(subject.Id);
                }
            }
            else
            {
                _logger.LogWarning("Specialized subject {SubjectCode} with ID {SubjectId} has a null semester and will not be included in any Quest Chapter.", subject.SubjectCode, subject.Id);
            }
        }

        var questChaptersToInsert = new List<QuestChapter>();
        // Create a temporary structure to hold chapter and its subjects before insertion.
        var chapterPreperationList = new List<(QuestChapter chapter, List<Guid> subjectIds)>();

        foreach (var (semesterNumber, subjectIds) in semesterSubjectsMap.OrderBy(kvp => kvp.Key))
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
            chapterPreperationList.Add((questChapter, subjectIds));
        }

        // STEP 4a: First, bulk insert all the parent QuestChapter objects.
        // Capture the result, which contains the entities with their final, database-assigned IDs.
        var persistedChapters = (await _questChapterRepository.AddRangeAsync(questChaptersToInsert, cancellationToken)).ToList();

        var questsToInsert = new List<Quest>();

        // STEP 4b: Now, iterate through the *persisted* chapters to create the child Quest objects.
        foreach (var persistedChapter in persistedChapters)
        {
            // Find the original list of subjects that belong to this chapter.
            var originalChapterData = chapterPreperationList.FirstOrDefault(p => p.chapter.Title == persistedChapter.Title);
            if (originalChapterData.subjectIds == null) continue;

            int questSequence = 1;
            foreach (var subjectId in originalChapterData.subjectIds)
            {
                var subjectDetails = routeSubjects.FirstOrDefault(s => s.Id == subjectId) ?? classSubjects.FirstOrDefault(s => s.Id == subjectId);
                if (subjectDetails != null)
                {
                    var quest = new Quest
                    {
                        Id = Guid.NewGuid(),
                        Title = subjectDetails.SubjectName,
                        Description = subjectDetails.Description ?? $"Embark on the quest for {subjectDetails.SubjectName}.",
                        QuestType = QuestType.Practice,
                        DifficultyLevel = DifficultyLevel.Beginner,
                        // CRITICAL FIX: Use the ID from the `persistedChapter`, not the temporary in-memory object.
                        QuestChapterId = persistedChapter.Id,
                        SubjectId = subjectId,
                        Sequence = questSequence++,
                        Status = QuestStatus.NotStarted,
                        CreatedBy = userProfile.AuthUserId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };
                    questsToInsert.Add(quest);
                }
            }
        }

        // STEP 4c: With the correct parent IDs set, now bulk insert all the child Quest objects.
        await _questRepository.AddRangeAsync(questsToInsert, cancellationToken);

        foreach (var quest in questsToInsert)
        {
            if (quest.SubjectId.HasValue &&
                subjectMap.TryGetValue(quest.SubjectId.Value, out var shouldGenerateSteps))
            {
                if (shouldGenerateSteps)
                {
                    _logger.LogInformation("Queuing background job for quest step generation for Quest {QuestId}", quest.Id);
                    _backgroundJobClient.Enqueue<IQuestStepGenerationService>(service =>
                        service.GenerateQuestStepsAsync(request.AuthUserId, quest.Id));
                }
            }
        }

        _logger.LogInformation("Successfully generated QuestLine {LearningPathId}", persistedLearningPath.Id);

        return new GenerateQuestLineResponse
        {
            LearningPathId = persistedLearningPath.Id,
        };
    }
}