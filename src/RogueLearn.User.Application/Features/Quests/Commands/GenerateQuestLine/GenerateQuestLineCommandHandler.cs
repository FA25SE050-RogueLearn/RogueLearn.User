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
    // REMOVED: ILearningPathRepository dependency (No DB table used)
    private readonly IQuestRepository _questRepository;
    private readonly IUserQuestAttemptRepository _userQuestAttemptRepository;
    private readonly IQuestDifficultyResolver _difficultyResolver;

    // NEW: Needed for predictive analysis
    private readonly ISubjectSkillMappingRepository _mappingRepository;
    private readonly ISkillDependencyRepository _skillDependencyRepository;
    private readonly IUserSkillRepository _userSkillRepository;

    private readonly ILogger<GenerateQuestLineCommandHandler> _logger;

    public GenerateQuestLineCommandHandler(
        IUserProfileRepository userProfileRepository,
        ISubjectRepository subjectRepository,
        IClassSpecializationSubjectRepository classSpecializationSubjectRepository,
        IStudentSemesterSubjectRepository studentSemesterSubjectRepository,
        // REMOVED: ILearningPathRepository
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

        // 4. Pre-fetch Skill Data for Predictive Analysis
        var userSkills = await _userSkillRepository.GetSkillsByAuthIdAsync(request.AuthUserId, cancellationToken);
        var userSkillMap = userSkills.ToDictionary(s => s.SkillId);

        // Fetch all skill mappings for these subjects to know what they require
        var allSubjectIds = idealSubjects.Select(s => s.Id).ToList();
        var allMappings = await _mappingRepository.GetMappingsBySubjectIdsAsync(allSubjectIds, cancellationToken);
        var mappingsBySubject = allMappings.GroupBy(m => m.SubjectId).ToDictionary(g => g.Key, g => g.ToList());

        var allDependencies = await _skillDependencyRepository.GetAllAsync(cancellationToken);

        // 5. The Core Loop: Link Subjects -> Quests & Assign Difficulty (Predictive Analysis)
        foreach (var subject in idealSubjects)
        {
            // A. Find the Master Quest
            var masterQuest = await _questRepository.GetActiveQuestBySubjectIdAsync(subject.Id, cancellationToken);

            // If admin hasn't generated a quest for this subject yet, skip it.
            if (masterQuest == null)
            {
                continue;
            }

            // B. Calculate Prerequisite Proficiency (The "Predictive" Part)
            double proficiency = 1.0; // Default to 100%

            if (mappingsBySubject.TryGetValue(subject.Id, out var subjectSkillMappings))
            {
                var targetSkillIds = subjectSkillMappings.Select(m => m.SkillId).ToList();

                // Find skills that are PREREQUISITES for the skills this subject teaches
                var prerequisites = allDependencies
                    .Where(d => targetSkillIds.Contains(d.SkillId))
                    .Select(d => d.PrerequisiteSkillId)
                    .Distinct()
                    .ToList();

                if (prerequisites.Any())
                {
                    int totalPrereqs = prerequisites.Count;
                    int metPrereqs = 0;

                    foreach (var pid in prerequisites)
                    {
                        // Check if user has leveled up this prerequisite skill
                        if (userSkillMap.TryGetValue(pid, out var us) && us.Level >= 3) // Level 3 = "Competent"
                        {
                            metPrereqs++;
                        }
                    }

                    proficiency = (double)metPrereqs / totalPrereqs;
                }
            }

            // C. Calculate Personalized Difficulty
            var gradeRecord = gradeRecords.FirstOrDefault(g => g.SubjectId == subject.Id);
            var difficultyInfo = _difficultyResolver.ResolveDifficulty(gradeRecord, proficiency);

            // D. Create or Update the User's Attempt (Status: NotStarted)
            var existingAttempt = await _userQuestAttemptRepository.FirstOrDefaultAsync(
                a => a.AuthUserId == request.AuthUserId && a.QuestId == masterQuest.Id,
                cancellationToken);

            if (existingAttempt == null)
            {
                // Create new attempt with predicted difficulty
                var newAttempt = new UserQuestAttempt
                {
                    AuthUserId = request.AuthUserId,
                    QuestId = masterQuest.Id,
                    // IMPORTANT: Set status to NotStarted so it shows up in lists but doesn't start timer
                    Status = QuestAttemptStatus.NotStarted,
                    AssignedDifficulty = difficultyInfo.ExpectedDifficulty, // Lock the prediction
                    Notes = difficultyInfo.DifficultyReason,
                    StartedAt = DateTimeOffset.UtcNow
                };
                await _userQuestAttemptRepository.AddAsync(newAttempt, cancellationToken);
                _logger.LogInformation("Generated NotStarted attempt for Quest {QuestId} (Difficulty: {Diff}).", masterQuest.Id, difficultyInfo.ExpectedDifficulty);
            }
            else
            {
                // If exists but hasn't really started, update the prediction
                if (existingAttempt.Status == QuestAttemptStatus.NotStarted)
                {
                    existingAttempt.AssignedDifficulty = difficultyInfo.ExpectedDifficulty;
                    existingAttempt.Notes = $"Difficulty updated (Preview): {difficultyInfo.DifficultyReason}";
                    await _userQuestAttemptRepository.UpdateAsync(existingAttempt, cancellationToken);
                }
            }
        }

        // Return AuthUserId as the virtual LearningPathId
        return new GenerateQuestLineResponse { LearningPathId = request.AuthUserId };
    }
}