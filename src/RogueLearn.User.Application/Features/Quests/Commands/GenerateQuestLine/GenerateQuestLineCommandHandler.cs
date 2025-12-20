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
    private readonly IQuestRepository _questRepository;
    private readonly IUserQuestAttemptRepository _userQuestAttemptRepository;
    private readonly IQuestDifficultyResolver _difficultyResolver;

    private readonly ISubjectSkillMappingRepository _mappingRepository;
    private readonly ISkillDependencyRepository _skillDependencyRepository;
    private readonly IUserSkillRepository _userSkillRepository;

    private readonly ILogger<GenerateQuestLineCommandHandler> _logger;

    public GenerateQuestLineCommandHandler(
        IUserProfileRepository userProfileRepository,
        ISubjectRepository subjectRepository,
        IClassSpecializationSubjectRepository classSpecializationSubjectRepository,
        IStudentSemesterSubjectRepository studentSemesterSubjectRepository,
        IQuestRepository questRepository,
        IUserQuestAttemptRepository userQuestAttemptRepository,
        IQuestDifficultyResolver difficultyResolver,
        ISubjectSkillMappingRepository mappingRepository,
        ISkillDependencyRepository skillDependencyRepository,
        IUserSkillRepository userSkillRepository,
        ILogger<GenerateQuestLineCommandHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _subjectRepository = subjectRepository;
        _classSpecializationSubjectRepository = classSpecializationSubjectRepository;
        _studentSemesterSubjectRepository = studentSemesterSubjectRepository;
        _questRepository = questRepository;
        _userQuestAttemptRepository = userQuestAttemptRepository;
        _difficultyResolver = difficultyResolver;
        _mappingRepository = mappingRepository;
        _skillDependencyRepository = skillDependencyRepository;
        _userSkillRepository = userSkillRepository;
        _logger = logger;
    }

    public async Task<GenerateQuestLineResponse> Handle(GenerateQuestLine request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating quest line for user {AuthUserId}", request.AuthUserId);

        // 1. Validation & Profile Loading
        var userProfile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken);
        if (userProfile is null) throw new BadRequestException("Invalid User Profile");
        if (userProfile.RouteId is null || userProfile.ClassId is null) throw new BadRequestException("Please select a route and class first.");

        // 2. Identify Target Subjects (From Route + Class)
        var routeSubjects = await _subjectRepository.GetSubjectsByRoute(userProfile.RouteId.Value, cancellationToken);
        var classSubjects = await _classSpecializationSubjectRepository.GetSubjectByClassIdAsync(userProfile.ClassId.Value, cancellationToken);

        var idealSubjects = routeSubjects.Concat(classSubjects)
            .DistinctBy(s => s.Id)
            .ToList();

        _logger.LogInformation("Found {Count} total subjects for user's academic path.", idealSubjects.Count);

        // 3. Get User's Academic History (Grades)
        var gradeRecords = (await _studentSemesterSubjectRepository.GetSemesterSubjectsByUserAsync(request.AuthUserId, cancellationToken)).ToList();

        // Map for quick lookup of "Did I pass Subject X?"
        var passedSubjectIds = gradeRecords
            .Where(r => r.Status == SubjectEnrollmentStatus.Passed)
            .Select(r => r.SubjectId)
            .ToHashSet();

        // 4. Pre-fetch Skill Data for Predictive Analysis
        var userSkills = await _userSkillRepository.GetSkillsByAuthIdAsync(request.AuthUserId, cancellationToken);
        var userSkillMap = userSkills.ToDictionary(s => s.SkillId);

        var allSubjectIds = idealSubjects.Select(s => s.Id).ToList();
        var allMappings = await _mappingRepository.GetMappingsBySubjectIdsAsync(allSubjectIds, cancellationToken);
        var mappingsBySubject = allMappings.GroupBy(m => m.SubjectId).ToDictionary(g => g.Key, g => g.ToList());

        var allDependencies = await _skillDependencyRepository.GetAllAsync(cancellationToken);

        // 5. The Core Loop: Intelligent Quest Generation
        int generatedCount = 0;
        int skippedCount = 0;

        foreach (var subject in idealSubjects)
        {
            // --- FILTER LOGIC: SHOULD THIS QUEST EXIST FOR THE USER? ---

            var gradeRecord = gradeRecords.FirstOrDefault(g => g.SubjectId == subject.Id);
            bool isStarted = gradeRecord != null;

            // Rule 1: Always generate if user has interacted with the subject (Studying, Passed, Failed)
            bool shouldGenerate = isStarted;

            // Rule 2: If not started, check if it is "Next Up" (Prerequisites met)
            if (!shouldGenerate)
            {
                // Check if all prerequisites are passed
                // Note: Subject.PrerequisiteSubjectIds is an array of Guids
                if (subject.PrerequisiteSubjectIds == null || subject.PrerequisiteSubjectIds.Length == 0)
                {
                    // No prereqs -> Available (e.g. Semester 1 subjects)
                    shouldGenerate = true;
                }
                else
                {
                    // Check if ALL prereqs are in the passed set
                    bool allPrereqsMet = subject.PrerequisiteSubjectIds.All(id => passedSubjectIds.Contains(id));
                    if (allPrereqsMet)
                    {
                        shouldGenerate = true;
                    }
                }
            }

            if (!shouldGenerate)
            {
                // Skip future content (e.g., Semester 9 Capstone when in Semester 2)
                skippedCount++;
                continue;
            }

            // --- END FILTER LOGIC ---

            // A. Find the Master Quest
            var masterQuest = await _questRepository.GetActiveQuestBySubjectIdAsync(subject.Id, cancellationToken);

            if (masterQuest == null)
            {
                continue;
            }

            // B. Calculate Prerequisite Proficiency (The "Predictive" Part)
            double proficiency = -1.0;

            if (mappingsBySubject.TryGetValue(subject.Id, out var subjectSkillMappings))
            {
                var targetSkillIds = subjectSkillMappings.Select(m => m.SkillId).ToList();
                var prerequisites = allDependencies
                    .Where(d => targetSkillIds.Contains(d.SkillId))
                    .Select(d => d.PrerequisiteSkillId)
                    .Distinct()
                    .ToList();

                if (prerequisites.Any())
                {
                    int totalPrereqs = prerequisites.Count;
                    int metPrereqs = 0;
                    int unknownPrereqs = 0;

                    foreach (var pid in prerequisites)
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

            // C. Calculate Personalized Difficulty
            var difficultyInfo = _difficultyResolver.ResolveDifficulty(gradeRecord, proficiency);

            // D. Create or Update the User's Attempt
            var existingAttempt = await _userQuestAttemptRepository.FirstOrDefaultAsync(
                a => a.AuthUserId == request.AuthUserId && a.QuestId == masterQuest.Id,
                cancellationToken);

            if (existingAttempt == null)
            {
                var newAttempt = new UserQuestAttempt
                {
                    AuthUserId = request.AuthUserId,
                    QuestId = masterQuest.Id,
                    Status = QuestAttemptStatus.NotStarted,
                    AssignedDifficulty = difficultyInfo.ExpectedDifficulty,
                    Notes = difficultyInfo.DifficultyReason,
                    StartedAt = DateTimeOffset.UtcNow
                };
                await _userQuestAttemptRepository.AddAsync(newAttempt, cancellationToken);
                generatedCount++;
            }
            else
            {
                if (existingAttempt.Status == QuestAttemptStatus.NotStarted)
                {
                    existingAttempt.AssignedDifficulty = difficultyInfo.ExpectedDifficulty;
                    existingAttempt.Notes = $"Difficulty updated (Preview): {difficultyInfo.DifficultyReason}";
                    await _userQuestAttemptRepository.UpdateAsync(existingAttempt, cancellationToken);
                }
            }
        }

        _logger.LogInformation("Quest generation complete. Generated/Updated: {Gen}, Skipped (Locked): {Skip}", generatedCount, skippedCount);

        return new GenerateQuestLineResponse { LearningPathId = request.AuthUserId };
    }
}