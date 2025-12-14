// RogueLearn.User/src/RogueLearn.User.Application/Features/LearningPaths/Queries/GetMyLearningPath/GetMyLearningPathQueryHandler.cs
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.LearningPaths.Queries.GetMyLearningPath;

public class GetMyLearningPathQueryHandler : IRequestHandler<GetMyLearningPathQuery, LearningPathDto?>
{
    private readonly ILearningPathRepository _learningPathRepository;
    private readonly IStudentSemesterSubjectRepository _studentSubjectRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ICurriculumProgramSubjectRepository _programSubjectRepository;
    private readonly IClassSpecializationSubjectRepository _classSpecializationSubjectRepository;
    // REMOVED: IQuestChapterRepository dependency
    private readonly ILogger<GetMyLearningPathQueryHandler> _logger;

    public GetMyLearningPathQueryHandler(
        ILearningPathRepository learningPathRepository,
        IStudentSemesterSubjectRepository studentSubjectRepository,
        ISubjectRepository subjectRepository,
        IQuestRepository questRepository,
        IUserQuestAttemptRepository attemptRepository,
        IUserProfileRepository userProfileRepository,
        ICurriculumProgramSubjectRepository programSubjectRepository,
        IClassSpecializationSubjectRepository classSpecializationSubjectRepository,
        // REMOVED: questChapterRepository
        ILogger<GetMyLearningPathQueryHandler> logger)
    {
        _learningPathRepository = learningPathRepository;
        _studentSubjectRepository = studentSubjectRepository;
        _subjectRepository = subjectRepository;
        _questRepository = questRepository;
        _attemptRepository = attemptRepository;
        _userProfileRepository = userProfileRepository;
        _programSubjectRepository = programSubjectRepository;
        _classSpecializationSubjectRepository = classSpecializationSubjectRepository;
        _logger = logger;
    }

    public async Task<LearningPathDto?> Handle(GetMyLearningPathQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching dynamic learning path for user {AuthUserId}", request.AuthUserId);

        // 1. Get User Profile
        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken);
        if (userProfile == null) return null;

        if (userProfile.RouteId == null || userProfile.ClassId == null)
        {
            return new LearningPathDto
            {
                Id = Guid.Empty,
                Name = "Unassigned Path",
                Description = "Please complete onboarding to view your learning path.",
                Chapters = new List<QuestChapterDto>(),
                CompletionPercentage = 0
            };
        }

        // 2. Fetch all subjects defined in the User's Curriculum
        var programSubjects = await _programSubjectRepository.FindAsync(ps => ps.ProgramId == userProfile.RouteId.Value, cancellationToken);
        var programSubjectIds = programSubjects.Select(ps => ps.SubjectId).ToHashSet();

        var classSubjects = await _classSpecializationSubjectRepository.GetSubjectByClassIdAsync(userProfile.ClassId.Value, cancellationToken);
        var classSubjectIds = classSubjects.Select(cs => cs.Id).ToHashSet();

        var allSubjectIds = programSubjectIds.Union(classSubjectIds).ToList();

        if (!allSubjectIds.Any())
        {
            return new LearningPathDto { Name = "Empty Curriculum", Chapters = new List<QuestChapterDto>() };
        }

        // 3. Fetch Master Subject Details
        var subjects = (await _subjectRepository.GetByIdsAsync(allSubjectIds, cancellationToken))
                       .ToDictionary(s => s.Id);

        // 4. Fetch User's Academic Progress
        var studentRecords = (await _studentSubjectRepository.FindAsync(
            ss => ss.AuthUserId == request.AuthUserId, cancellationToken))
            .ToDictionary(ss => ss.SubjectId);

        // 5. Fetch Master Quests
        var masterQuests = (await _questRepository.GetAllAsync(cancellationToken))
            .Where(q => q.SubjectId.HasValue && allSubjectIds.Contains(q.SubjectId.Value) && q.IsActive)
            .ToDictionary(q => q.SubjectId!.Value);

        // 6. Fetch User's Attempts (Contains the AssignedDifficulty set by the Command)
        var attempts = (await _attemptRepository.FindAsync(
            a => a.AuthUserId == request.AuthUserId, cancellationToken))
            .GroupBy(a => a.QuestId)
            .ToDictionary(g => g.Key, g => g.First());

        // 7. Get (or stub) Learning Path metadata
        var learningPath = await _learningPathRepository.GetLatestByUserAsync(request.AuthUserId, cancellationToken);
        var learningPathId = learningPath?.Id ?? Guid.NewGuid();

        // 8. Build Chapters dynamically based on Semester
        // This effectively replaces the logic of the 'quest_chapters' table
        var semesterGroups = subjects.Values
            .GroupBy(s => s.Semester ?? 0)
            .OrderBy(g => g.Key)
            .ToList();

        var chapterDtos = new List<QuestChapterDto>();
        int completedQuestsCount = 0;
        int totalQuestsCount = 0;

        foreach (var group in semesterGroups)
        {
            var semester = group.Key;
            var chapterId = Guid.NewGuid(); // Virtual ID since table is gone

            var chapterDto = new QuestChapterDto
            {
                Id = chapterId,
                Title = semester == 0 ? "Electives / Unassigned" : $"Semester {semester}",
                Sequence = semester,
                Status = "NotStarted"
            };

            foreach (var subject in group)
            {
                if (masterQuests.TryGetValue(subject.Id, out var masterQuest))
                {
                    totalQuestsCount++;
                    var attempt = attempts.GetValueOrDefault(masterQuest.Id);
                    var studentRecord = studentRecords.GetValueOrDefault(subject.Id);

                    // PRESERVED GRADING LOGIC FOR STATUS:
                    // If no attempt, but academic record says Passed -> Mark as Completed
                    var computedStatus = attempt?.Status.ToString() ??
                                 (studentRecord?.Status == SubjectEnrollmentStatus.Passed ? "Completed" : "NotStarted");

                    // PRESERVED GRADING LOGIC FOR DIFFICULTY:
                    // Read 'AssignedDifficulty' from the attempt (calculated in Command), or fallback to MasterQuest default
                    var displayDifficulty = attempt?.AssignedDifficulty ?? masterQuest.ExpectedDifficulty ?? "Standard";

                    var questDto = new QuestSummaryDto
                    {
                        Id = masterQuest.Id,
                        Title = masterQuest.Title,
                        SubjectId = subject.Id,
                        Status = computedStatus,
                        ExpectedDifficulty = displayDifficulty,
                        DifficultyReason = attempt?.Notes ?? masterQuest.DifficultyReason,
                        IsRecommended = masterQuest.IsRecommended,
                        RecommendationReason = masterQuest.RecommendationReason,
                        SubjectGrade = studentRecord?.Grade,
                        SubjectStatus = studentRecord?.Status.ToString() ?? "NotStarted",
                        LearningPathId = learningPathId,
                        ChapterId = chapterId
                    };

                    chapterDto.Quests.Add(questDto);

                    if (questDto.Status == "Completed") completedQuestsCount++;
                }
            }

            if (chapterDto.Quests.Any())
            {
                if (chapterDto.Quests.All(q => q.Status == "Completed"))
                    chapterDto.Status = "Completed";
                else if (chapterDto.Quests.Any(q => q.Status == "InProgress" || q.Status == "Completed"))
                    chapterDto.Status = "InProgress";

                chapterDtos.Add(chapterDto);
            }
        }

        var completionPercentage = totalQuestsCount > 0
            ? Math.Round((double)completedQuestsCount / totalQuestsCount * 100, 2)
            : 0;

        return new LearningPathDto
        {
            Id = learningPathId,
            Name = learningPath?.Name ?? "My Academic Journey",
            Description = learningPath?.Description ?? $"Personalized curriculum for {userProfile.Username}.",
            Chapters = chapterDtos,
            CompletionPercentage = completionPercentage
        };
    }
}