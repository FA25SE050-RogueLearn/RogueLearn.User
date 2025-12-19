// RogueLearn.User/src/RogueLearn.User.Application/Features/LearningPaths/Queries/GetMyLearningPath/GetMyLearningPathQueryHandler.cs
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.LearningPaths.Queries.GetMyLearningPath;

public class GetMyLearningPathQueryHandler : IRequestHandler<GetMyLearningPathQuery, LearningPathDto?>
{
    private readonly IStudentSemesterSubjectRepository _studentSubjectRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ICurriculumProgramSubjectRepository _programSubjectRepository;
    private readonly IClassSpecializationSubjectRepository _classSpecializationSubjectRepository;
    private readonly IQuestDifficultyResolver _difficultyResolver;

    // NEW: Dependencies for accurate preview calculation
    private readonly ISubjectSkillMappingRepository _mappingRepository;
    private readonly ISkillDependencyRepository _skillDependencyRepository;
    private readonly IUserSkillRepository _userSkillRepository;

    private readonly ILogger<GetMyLearningPathQueryHandler> _logger;

    public GetMyLearningPathQueryHandler(
        IStudentSemesterSubjectRepository studentSubjectRepository,
        ISubjectRepository subjectRepository,
        IQuestRepository questRepository,
        IUserQuestAttemptRepository attemptRepository,
        IUserProfileRepository userProfileRepository,
        ICurriculumProgramSubjectRepository programSubjectRepository,
        IClassSpecializationSubjectRepository classSpecializationSubjectRepository,
        IQuestDifficultyResolver difficultyResolver,
        ISubjectSkillMappingRepository mappingRepository,
        ISkillDependencyRepository skillDependencyRepository,
        IUserSkillRepository userSkillRepository,
        ILogger<GetMyLearningPathQueryHandler> logger)
    {
        _studentSubjectRepository = studentSubjectRepository;
        _subjectRepository = subjectRepository;
        _questRepository = questRepository;
        _attemptRepository = attemptRepository;
        _userProfileRepository = userProfileRepository;
        _programSubjectRepository = programSubjectRepository;
        _classSpecializationSubjectRepository = classSpecializationSubjectRepository;
        _difficultyResolver = difficultyResolver;
        _mappingRepository = mappingRepository;
        _skillDependencyRepository = skillDependencyRepository;
        _userSkillRepository = userSkillRepository;
        _logger = logger;
    }

    public async Task<LearningPathDto?> Handle(GetMyLearningPathQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching dynamic learning path for user {AuthUserId}", request.AuthUserId);

        // 1. Get User Profile
        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken);
        if (userProfile == null) return null;

        var virtualPathId = userProfile.AuthUserId;
        var virtualPathName = string.IsNullOrWhiteSpace(userProfile.FirstName)
            ? "My Learning Path"
            : $"{userProfile.FirstName}'s Journey";

        if (userProfile.RouteId == null || userProfile.ClassId == null)
        {
            return new LearningPathDto
            {
                Id = virtualPathId,
                Name = "Unassigned Path",
                Description = "Please complete onboarding to view your learning path.",
                Chapters = new List<QuestChapterDto>(),
                CompletionPercentage = 0
            };
        }

        // 2. Fetch all subjects
        var programSubjects = await _programSubjectRepository.FindAsync(ps => ps.ProgramId == userProfile.RouteId.Value, cancellationToken);
        var programSubjectIds = programSubjects.Select(ps => ps.SubjectId).ToHashSet();

        var classSubjects = await _classSpecializationSubjectRepository.GetSubjectByClassIdAsync(userProfile.ClassId.Value, cancellationToken);
        var classSubjectIds = classSubjects.Select(cs => cs.Id).ToHashSet();

        var allSubjectIds = programSubjectIds.Union(classSubjectIds).ToList();

        if (!allSubjectIds.Any())
        {
            return new LearningPathDto
            {
                Id = virtualPathId,
                Name = virtualPathName,
                Description = "No subjects found in your current curriculum.",
                Chapters = new List<QuestChapterDto>()
            };
        }

        // 3. Fetch Master Data
        var subjects = (await _subjectRepository.GetByIdsAsync(allSubjectIds, cancellationToken))
                       .ToDictionary(s => s.Id);

        var studentRecords = (await _studentSubjectRepository.FindAsync(
            ss => ss.AuthUserId == request.AuthUserId, cancellationToken))
            .ToDictionary(ss => ss.SubjectId);

        var masterQuests = (await _questRepository.GetAllAsync(cancellationToken))
            .Where(q => q.SubjectId.HasValue && allSubjectIds.Contains(q.SubjectId.Value) && q.IsActive)
            .ToDictionary(q => q.SubjectId!.Value);

        var attempts = (await _attemptRepository.FindAsync(
            a => a.AuthUserId == request.AuthUserId, cancellationToken))
            .GroupBy(a => a.QuestId)
            .ToDictionary(g => g.Key, g => g.First());

        // 4. Pre-fetch Skill Data for Efficient Calculation
        var userSkills = await _userSkillRepository.GetSkillsByAuthIdAsync(request.AuthUserId, cancellationToken);
        var userSkillMap = userSkills.ToDictionary(s => s.SkillId);

        var allDependencies = await _skillDependencyRepository.GetAllAsync(cancellationToken);

        // Fetch ALL mappings for ALL subjects in the path to avoid N+1
        var allMappings = await _mappingRepository.GetMappingsBySubjectIdsAsync(allSubjectIds, cancellationToken);
        var subjectSkillMap = allMappings.GroupBy(m => m.SubjectId).ToDictionary(g => g.Key, g => g.ToList());

        // 5. Build Structure
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
            var chapterId = Guid.NewGuid();

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

                    // Determine Status
                    var computedStatus = attempt?.Status.ToString() ??
                                 (studentRecord?.Status == SubjectEnrollmentStatus.Passed ? "Completed" : "NotStarted");

                    string displayDifficulty;
                    string diffReason;

                    // CHECK IF LOCKED
                    bool isLocked = attempt != null &&
                                   attempt.Status != QuestAttemptStatus.NotStarted &&
                                   !string.IsNullOrEmpty(attempt.AssignedDifficulty);

                    if (isLocked)
                    {
                        displayDifficulty = attempt!.AssignedDifficulty;
                        diffReason = "Locked to your initial assessment";
                    }
                    else
                    {
                        // FALLBACK: Dynamic calculation (Preview only)
                        // Calculate REAL prerequisite proficiency to match StartQuest logic
                        double proficiency = 1.0;
                        if (subjectSkillMap.TryGetValue(subject.Id, out var subjectMappings) && subjectMappings.Any())
                        {
                            var targetSkillIds = subjectMappings.Select(m => m.SkillId).ToList();
                            var prereqs = allDependencies
                                .Where(d => targetSkillIds.Contains(d.SkillId))
                                .Select(d => d.PrerequisiteSkillId)
                                .Distinct()
                                .ToList();

                            if (prereqs.Any())
                            {
                                int met = 0;
                                foreach (var pid in prereqs)
                                {
                                    if (userSkillMap.TryGetValue(pid, out var us) && us.Level >= 3)
                                        met++;
                                }
                                proficiency = (double)met / prereqs.Count;
                            }
                        }

                        var diffInfo = _difficultyResolver.ResolveDifficulty(studentRecord, proficiency);
                        displayDifficulty = diffInfo.ExpectedDifficulty;
                        diffReason = diffInfo.DifficultyReason;
                    }

                    var questDto = new QuestSummaryDto
                    {
                        Id = masterQuest.Id,
                        Title = masterQuest.Title,
                        SubjectId = subject.Id,
                        Status = computedStatus,
                        ExpectedDifficulty = displayDifficulty,
                        DifficultyReason = diffReason,
                        IsRecommended = masterQuest.IsRecommended,
                        RecommendationReason = masterQuest.RecommendationReason,
                        SubjectGrade = studentRecord?.Grade,
                        SubjectStatus = studentRecord?.Status.ToString() ?? "NotStarted",
                        LearningPathId = virtualPathId,
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
            Id = virtualPathId,
            Name = virtualPathName,
            Description = $"Personalized curriculum for {userProfile.Username}.",
            Chapters = chapterDtos,
            CompletionPercentage = completionPercentage
        };
    }
}