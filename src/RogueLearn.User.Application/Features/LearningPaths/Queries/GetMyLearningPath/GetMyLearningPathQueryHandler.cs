// RogueLearn.User/src/RogueLearn.User.Application/Features/LearningPaths/Queries/GetMyLearningPath/GetMyLearningPathQueryHandler.cs
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<GetMyLearningPathQueryHandler> _logger;

    public GetMyLearningPathQueryHandler(
        ILearningPathRepository learningPathRepository,
        IStudentSemesterSubjectRepository studentSubjectRepository,
        ISubjectRepository subjectRepository,
        IQuestRepository questRepository,
        IUserQuestAttemptRepository attemptRepository,
        ILogger<GetMyLearningPathQueryHandler> logger)
    {
        _learningPathRepository = learningPathRepository;
        _studentSubjectRepository = studentSubjectRepository;
        _subjectRepository = subjectRepository;
        _questRepository = questRepository;
        _attemptRepository = attemptRepository;
        _logger = logger;
    }

    public async Task<LearningPathDto?> Handle(GetMyLearningPathQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching dynamic learning path for user {AuthUserId}", request.AuthUserId);

        // 1. Get the shell learning path (mostly for metadata like Name/Description)
        var learningPath = await _learningPathRepository.GetLatestByUserAsync(request.AuthUserId, cancellationToken);
        if (learningPath == null)
        {
            // Create a virtual default if none exists
            learningPath = new LearningPath
            {
                Id = Guid.NewGuid(),
                Name = "My Academic Journey",
                Description = "Personalized curriculum based on your academic record."
            };
        }

        // 2. Fetch User's Academic Record (The Source of Truth for the Path)
        var studentSubjects = (await _studentSubjectRepository.FindAsync(
            ss => ss.AuthUserId == request.AuthUserId,
            cancellationToken)).ToList();

        if (!studentSubjects.Any())
        {
            return new LearningPathDto
            {
                Id = learningPath.Id,
                Name = learningPath.Name,
                Description = learningPath.Description,
                Chapters = new List<QuestChapterDto>(),
                CompletionPercentage = 0
            };
        }

        // 3. Fetch related Master Subjects
        var subjectIds = studentSubjects.Select(ss => ss.SubjectId).Distinct().ToList();
        var subjects = (await _subjectRepository.GetByIdsAsync(subjectIds, cancellationToken)).ToDictionary(s => s.Id);

        // 4. Fetch Master Quests for these subjects
        // We assume 1-to-1 mapping: Subject -> Master Quest
        var masterQuests = (await _questRepository.GetAllAsync(cancellationToken))
            .Where(q => q.SubjectId.HasValue && subjectIds.Contains(q.SubjectId.Value) && q.IsActive)
            .ToDictionary(q => q.SubjectId!.Value);

        // 5. Fetch User's Attempts (Progress & Difficulty)
        var attempts = (await _attemptRepository.FindAsync(
            a => a.AuthUserId == request.AuthUserId,
            cancellationToken)).ToList();

        // Map QuestId -> Attempt
        var attemptMap = attempts.GroupBy(a => a.QuestId).ToDictionary(g => g.Key, g => g.First());

        // 6. Build Chapters dynamically based on Semester
        // Group by Semester (from Subject)
        var semesterGroups = studentSubjects
            .Where(ss => subjects.ContainsKey(ss.SubjectId))
            .GroupBy(ss => subjects[ss.SubjectId].Semester ?? 0) // Default to Semester 0 if null
            .OrderBy(g => g.Key)
            .ToList();

        var chapterDtos = new List<QuestChapterDto>();
        int completedQuestsCount = 0;
        int totalQuestsCount = 0;

        foreach (var group in semesterGroups)
        {
            var semester = group.Key;
            var chapterId = Guid.NewGuid(); // Virtual ID for UI stability

            var chapterDto = new QuestChapterDto
            {
                Id = chapterId,
                Title = semester == 0 ? "Electives / Unassigned" : $"Semester {semester}",
                Sequence = semester,
                Status = "NotStarted" // Calculated below
            };

            foreach (var ss in group)
            {
                if (!subjects.TryGetValue(ss.SubjectId, out var subject)) continue;

                // Is there a quest for this subject?
                if (masterQuests.TryGetValue(subject.Id, out var masterQuest))
                {
                    totalQuestsCount++;
                    var attempt = attemptMap.GetValueOrDefault(masterQuest.Id);

                    var questDto = new QuestSummaryDto
                    {
                        Id = masterQuest.Id,
                        Title = masterQuest.Title, // e.g., "PRO192: Java Programming"
                        SubjectId = subject.Id,

                        // Status logic: Check Attempt first, then fallback to NotStarted
                        Status = attempt?.Status.ToString() ?? "NotStarted",

                        // Difficulty logic: Check Attempt first (Runtime Personalization), then Master Quest default
                        ExpectedDifficulty = attempt?.AssignedDifficulty ?? masterQuest.ExpectedDifficulty ?? "Standard",
                        DifficultyReason = attempt?.Notes ?? masterQuest.DifficultyReason,

                        // Recommendation logic
                        IsRecommended = masterQuest.IsRecommended, // This might need per-user calculation if stored on Master
                        RecommendationReason = masterQuest.RecommendationReason,

                        SubjectGrade = ss.Grade,
                        SubjectStatus = ss.Status.ToString(),

                        LearningPathId = learningPath.Id,
                        ChapterId = chapterId
                    };

                    chapterDto.Quests.Add(questDto);

                    if (questDto.Status == "Completed") completedQuestsCount++;
                }
            }

            // Calculate Chapter Status
            if (chapterDto.Quests.Any())
            {
                if (chapterDto.Quests.All(q => q.Status == "Completed"))
                    chapterDto.Status = "Completed";
                else if (chapterDto.Quests.Any(q => q.Status == "InProgress" || q.Status == "Completed"))
                    chapterDto.Status = "InProgress";
            }

            chapterDtos.Add(chapterDto);
        }

        var completionPercentage = totalQuestsCount > 0
            ? Math.Round((double)completedQuestsCount / totalQuestsCount * 100, 2)
            : 0;

        return new LearningPathDto
        {
            Id = learningPath.Id,
            Name = learningPath.Name,
            Description = learningPath.Description,
            Chapters = chapterDtos,
            CompletionPercentage = completionPercentage
        };
    }
}