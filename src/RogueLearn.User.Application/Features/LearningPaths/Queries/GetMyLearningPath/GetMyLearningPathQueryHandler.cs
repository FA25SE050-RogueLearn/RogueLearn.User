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
        // 1. Get User Profile to determine their academic route and specialization.
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

        // 2. Fetch all subjects relevant to the user's main program and specialization class.
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

        // 3. Fetch all necessary master data in bulk to avoid N+1 queries in the loop.
        // This includes all subjects, student grade records, master quests, and quest attempts.
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

        // 4. Pre-fetch skill-related data for live "preview" difficulty calculations.
        var userSkills = await _userSkillRepository.GetSkillsByAuthIdAsync(request.AuthUserId, cancellationToken);
        var userSkillMap = userSkills.ToDictionary(s => s.SkillId);

        var allDependencies = await _skillDependencyRepository.GetAllAsync(cancellationToken);
        var allMappings = await _mappingRepository.GetMappingsBySubjectIdsAsync(allSubjectIds, cancellationToken);
        var subjectSkillMap = allMappings.GroupBy(m => m.SubjectId).ToDictionary(g => g.Key, g => g.ToList());

        // 5. Build the learning path structure, grouped by semester.
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

                    // Determine the quest's status by checking the attempt first, then falling back to the academic record.
                    var computedStatus = attempt?.Status.ToString() ??
                                 (studentRecord?.Status == SubjectEnrollmentStatus.Passed ? "Completed" : "NotStarted");

                    string displayDifficulty;
                    string diffReason;

                    // This is the core logic for displaying the correct, updated difficulty.
                    // It prioritizes the 'assigned_difficulty' stored in the user's quest attempt record.
                    // This record is updated by the 'ProcessAcademicRecord' flow whenever the user's GPA changes.
                    if (attempt != null && !string.IsNullOrEmpty(attempt.AssignedDifficulty))
                    {
                        // Use the difficulty that was calculated and saved during the last academic sync.
                        displayDifficulty = attempt.AssignedDifficulty;

                        // Display a different reason based on whether the user has started the quest.
                        if (attempt.Status != QuestAttemptStatus.NotStarted)
                        {
                            diffReason = "Locked to your initial assessment";
                        }
                        else
                        {
                            // If the quest is not started, display the reason saved in the 'notes' field,
                            // which explains why the AI predicted this difficulty.
                            diffReason = !string.IsNullOrEmpty(attempt.Notes)
                                ? attempt.Notes
                                : "Personalized based on academic analysis";
                        }
                    }
                    else
                    {
                        // This fallback block is for generating a "live preview" of difficulty
                        // for quests that do not have an attempt record yet (e.g., future quests).
                        // It does not use the full AI analysis for performance reasons.
                        double proficiency = -1.0;

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
                                int totalPrereqs = prereqs.Count;
                                int metPrereqs = 0;
                                int unknownPrereqs = 0;

                                foreach (var pid in prereqs)
                                {
                                    if (userSkillMap.TryGetValue(pid, out var us))
                                    {
                                        if (us.Level >= 2) metPrereqs++;
                                    }
                                    else
                                    {
                                        unknownPrereqs++;
                                    }
                                }

                                if (unknownPrereqs == totalPrereqs)
                                {
                                    proficiency = 1.0;
                                }
                                else
                                {
                                    proficiency = (double)(metPrereqs + unknownPrereqs) / totalPrereqs;
                                }
                            }
                        }

                        // Calculate a preview difficulty without the full AI analysis report.
                        var diffInfo = _difficultyResolver.ResolveDifficulty(
                            studentRecord,
                            proficiency,
                            subject,
                            null,  // AI Report is not available in this live query.
                            null);

                        displayDifficulty = diffInfo.ExpectedDifficulty;
                        diffReason = diffInfo.DifficultyReason;
                    }

                    // Assemble the Quest DTO with the determined difficulty and status.
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

            // Determine the overall status of the chapter (semester).
            if (chapterDto.Quests.Any())
            {
                if (chapterDto.Quests.All(q => q.Status == "Completed"))
                    chapterDto.Status = "Completed";
                else if (chapterDto.Quests.Any(q => q.Status == "InProgress" || q.Status == "Completed"))
                    chapterDto.Status = "InProgress";

                chapterDtos.Add(chapterDto);
            }
        }

        // Calculate the overall completion percentage for the entire learning path.
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