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
    private readonly ISkillRepository _skillRepository;

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
        ISkillRepository skillRepository,
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
        _skillRepository = skillRepository;
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
        var passedSubjectIds = gradeRecords
            .Where(r => r.Status == SubjectEnrollmentStatus.Passed)
            .Select(r => r.SubjectId)
            .ToHashSet();

        // 4. Pre-fetch Skill Data
        var userSkills = await _userSkillRepository.GetSkillsByAuthIdAsync(request.AuthUserId, cancellationToken);
        var userSkillMap = userSkills.ToDictionary(s => s.SkillId);

        var allSubjectIds = idealSubjects.Select(s => s.Id).ToList();
        var allMappings = await _mappingRepository.GetMappingsBySubjectIdsAsync(allSubjectIds, cancellationToken);
        var mappingsBySubject = allMappings.GroupBy(m => m.SubjectId).ToDictionary(g => g.Key, g => g.ToList());

        // PRE-FETCH SKILL NAMES for Text Matching
        var allSkillIds = allMappings.Select(m => m.SkillId).Distinct().ToList();
        var skillDefinitions = (await _skillRepository.GetAllAsync(cancellationToken))
            .Where(s => allSkillIds.Contains(s.Id))
            .ToDictionary(s => s.Id, s => s.Name);

        var allDependencies = await _skillDependencyRepository.GetAllAsync(cancellationToken);

        // 5. The Core Loop: Intelligent Quest Generation
        int generatedCount = 0;
        int skippedCount = 0;
        int difficultyAdjustedCount = 0;

        foreach (var subject in idealSubjects)
        {
            var gradeRecord = gradeRecords.FirstOrDefault(g => g.SubjectId == subject.Id);
            bool isStarted = gradeRecord != null;
            bool shouldGenerate = isStarted;

            if (!shouldGenerate)
            {
                if (subject.PrerequisiteSubjectIds == null || subject.PrerequisiteSubjectIds.Length == 0)
                {
                    shouldGenerate = true;
                }
                else
                {
                    bool allPrereqsMet = subject.PrerequisiteSubjectIds.All(id => passedSubjectIds.Contains(id));
                    if (allPrereqsMet)
                    {
                        shouldGenerate = true;
                    }
                }
            }

            if (!shouldGenerate)
            {
                // _logger.LogDebug("Skipping subject {Code} - Prerequisites not met or future semester.", subject.SubjectCode);
                skippedCount++;
                continue;
            }

            var masterQuest = await _questRepository.GetActiveQuestBySubjectIdAsync(subject.Id, cancellationToken);
            if (masterQuest == null)
            {
                // _logger.LogWarning("No Master Quest found for subject {Code} ({Id}). Skipping.", subject.SubjectCode, subject.Id);
                continue;
            }

            // B. Calculate Prerequisite Proficiency
            double proficiency = -1.0;
            List<string> subjectSkillNames = new();

            if (mappingsBySubject.TryGetValue(subject.Id, out var subjectSkillMappings))
            {
                foreach (var mapping in subjectSkillMappings)
                {
                    if (skillDefinitions.TryGetValue(mapping.SkillId, out var skillName))
                    {
                        subjectSkillNames.Add(skillName);
                    }
                }

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

            // C. Calculate Personalized Difficulty with Logging
            var difficultyInfo = _difficultyResolver.ResolveDifficulty(
                gradeRecord,
                proficiency,
                subject,
                request.AiAnalysisReport,
                subjectSkillNames);

            // Log if AI influenced the difficulty
            if (difficultyInfo.DifficultyReason.Contains("Aligned with identified", StringComparison.OrdinalIgnoreCase))
            {
                difficultyAdjustedCount++;
                _logger.LogInformation("🤖 AI ADJUSTMENT: Quest '{SubjectCode}' ({SubjectName}) set to '{Difficulty}' based on analysis. Skills checked: [{Skills}]",
                    subject.SubjectCode, subject.SubjectName, difficultyInfo.ExpectedDifficulty, string.Join(", ", subjectSkillNames));
            }

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
                // Force update difficulty if it's still NotStarted (Preview Mode)
                if (existingAttempt.Status == QuestAttemptStatus.NotStarted)
                {
                    if (existingAttempt.AssignedDifficulty != difficultyInfo.ExpectedDifficulty)
                    {
                        _logger.LogInformation("Quest '{SubjectCode}' difficulty UPDATED: {OldDiff} -> {NewDiff} ({Reason})",
                            subject.SubjectCode, existingAttempt.AssignedDifficulty, difficultyInfo.ExpectedDifficulty, difficultyInfo.DifficultyReason);
                    }

                    existingAttempt.AssignedDifficulty = difficultyInfo.ExpectedDifficulty;
                    existingAttempt.Notes = $"Difficulty updated (Preview): {difficultyInfo.DifficultyReason}";
                    existingAttempt.UpdatedAt = DateTimeOffset.UtcNow; // Ensure timestamp updates
                    await _userQuestAttemptRepository.UpdateAsync(existingAttempt, cancellationToken);
                }
                else
                {
                    // Log that we skipped update because it's already active
                    // _logger.LogDebug("Skipping difficulty update for {Code}: Status is {Status}", subject.SubjectCode, existingAttempt.Status);
                }
            }
        }

        if (difficultyAdjustedCount == 0)
        {
            _logger.LogInformation("ℹ️ No quests were adjusted based on AI analysis. Difficulty defaulted to Standard/Prereq-based logic.");
        }
        else
        {
            _logger.LogInformation("✅ AI Analysis influenced {Count} quest difficulties.", difficultyAdjustedCount);
        }

        _logger.LogInformation("Quest generation complete. Generated/Updated: {Gen}, Skipped (Locked): {Skip}", generatedCount, skippedCount);

        return new GenerateQuestLineResponse { LearningPathId = request.AuthUserId };
    }
}