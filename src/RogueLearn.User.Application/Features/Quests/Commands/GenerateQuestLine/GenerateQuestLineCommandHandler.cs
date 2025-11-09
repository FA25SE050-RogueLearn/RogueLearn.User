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

        // 1. Create Learning Path
        var learningPath = new LearningPath
        {
            Name = $"{userProfile.Username}'s Path of Knowledge",
            Description = $"Your learning journey awaits!",
            PathType = PathType.Course,
            IsPublished = true, // Auto-publish for the user,
            CreatedBy = userProfile.AuthUserId
        };
        
        await _learningPathRepository.AddAsync(learningPath, cancellationToken);

        
        
        // contains all subjects but no specialized subjects (PRN, JAVA,...)
        var routeSubjects = await _subjectRepository.GetSubjectsByRoute(userProfile.RouteId.Value, cancellationToken);
        
        var subjectMap = routeSubjects.ToDictionary(
            subject => subject.Id,
            subject => false  // no quest step generation by default
        ); 
        
        // specialized subjects (PRN, JAVA,...)
        var classSubjects = await _classSpecializationSubjectRepository.GetSubjectByClassIdAsync(userProfile.ClassId.Value, cancellationToken);
        
        foreach (var subject in classSubjects)
        {
            // no quest step generation by default
            subjectMap[subject.Id] = false;
        } 
        
        // get all user learned/learning/failed subjects for mapping true, false
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
                subjectMap[subject.Id] = true;
            }
        }
        
        // Create semester to subjects mapping
        var semesterSubjectsMap = routeSubjects
            .GroupBy(subject => subject.Semester)
            .ToDictionary(
                group => group.Key,
                group => group.Select(subject => subject.Id).ToList()
            );

        // Add specialized subjects to their respective semesters
        foreach (var subject in classSubjects)
        {
            if (!semesterSubjectsMap.ContainsKey(subject.Semester))
            {
                semesterSubjectsMap[subject.Semester] = new List<Guid>();
            }
    
            if (!semesterSubjectsMap[subject.Semester].Contains(subject.Id))
            {
                semesterSubjectsMap[subject.Semester].Add(subject.Id);
            }
        }
        
        
        // 3. Create Quest Chapters and Quests
        
        var questChaptersToInsert = new List<QuestChapter>();
        var questsToInsert = new List<Quest>();

        // Prepare all quest chapters
        foreach (var (semesterNumber, subjectIds) in semesterSubjectsMap.OrderBy(kvp => kvp.Key))
        {
            var questChapter = new QuestChapter
            {
                Id = Guid.NewGuid(),
                LearningPathId = learningPath.Id,
                Title = $"Semester {semesterNumber}",
                Sequence = semesterNumber,
                Status = PathProgressStatus.NotStarted,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            questChaptersToInsert.Add(questChapter);

            // Prepare quests for this chapter
            int questSequence = 1;
            foreach (var subjectId in subjectIds)
            {
                var quest = new Quest
                {
                    Id = Guid.NewGuid(),
                    QuestChapterId = questChapter.Id, // Use the pre-generated ID
                    SubjectId = subjectId,
                    Sequence = questSequence++,
                    Status = QuestStatus.NotStarted,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                questsToInsert.Add(quest);
            }
        }

        // Bulk insert all quest chapters
        await _questChapterRepository.AddRangeAsync(questChaptersToInsert, cancellationToken);

        // Bulk insert all quests
        await _questRepository.AddRangeAsync(questsToInsert, cancellationToken);

        // Generate steps for Studying || Not Passed Quests
        foreach (var quest in questsToInsert)
        {
            if (quest.SubjectId.HasValue &&
                subjectMap.TryGetValue(quest.SubjectId.Value, out var shouldGenerateSteps))
            {
                if (shouldGenerateSteps)
                {
                    _logger.LogInformation("Queuing quest step generation for Quest {QuestId}", quest.Id);

                    // Enqueue the job - it will run in the background
                    _backgroundJobClient.Enqueue<IQuestStepGenerationService>(service =>
                        service.GenerateQuestStepsAsync(request.AuthUserId, quest.Id));
                }
            }
        }

        _logger.LogInformation("Successfully generated QuestLine {LearningPathId}", learningPath.Id);
        
        return new GenerateQuestLineResponse
        {
            LearningPathId = learningPath.Id,
        };
    }
}
