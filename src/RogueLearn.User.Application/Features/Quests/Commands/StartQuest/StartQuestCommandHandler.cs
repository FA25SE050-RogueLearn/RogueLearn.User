// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/StartQuest/StartQuestCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Quests.Commands.StartQuest;

public class StartQuestCommandHandler : IRequestHandler<StartQuestCommand, StartQuestResponse>
{
    private readonly IUserQuestAttemptRepository _attemptRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IStudentSemesterSubjectRepository _studentSubjectRepository;
    private readonly IQuestDifficultyResolver _difficultyResolver;

    // NEW DEPENDENCIES for Skill Analysis
    private readonly ISubjectSkillMappingRepository _mappingRepository;
    private readonly ISkillDependencyRepository _skillDependencyRepository;
    private readonly IUserSkillRepository _userSkillRepository;

    private readonly ILogger<StartQuestCommandHandler> _logger;

    public StartQuestCommandHandler(
        IUserQuestAttemptRepository attemptRepository,
        IQuestRepository questRepository,
        IStudentSemesterSubjectRepository studentSubjectRepository,
        IQuestDifficultyResolver difficultyResolver,
        ISubjectSkillMappingRepository mappingRepository,
        ISkillDependencyRepository skillDependencyRepository,
        IUserSkillRepository userSkillRepository,
        ILogger<StartQuestCommandHandler> logger)
    {
        _attemptRepository = attemptRepository;
        _questRepository = questRepository;
        _studentSubjectRepository = studentSubjectRepository;
        _difficultyResolver = difficultyResolver;
        _mappingRepository = mappingRepository;
        _skillDependencyRepository = skillDependencyRepository;
        _userSkillRepository = userSkillRepository;
        _logger = logger;
    }

    public async Task<StartQuestResponse> Handle(StartQuestCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("StartQuest: User {UserId} requesting to start quest {QuestId}", request.AuthUserId, request.QuestId);

        var quest = await _questRepository.GetByIdAsync(request.QuestId, cancellationToken);
        if (quest == null) throw new NotFoundException("Quest", request.QuestId);

        var existingAttempt = await _attemptRepository.FirstOrDefaultAsync(
            a => a.AuthUserId == request.AuthUserId && a.QuestId == request.QuestId,
            cancellationToken);

        // --- SKILL & PREREQUISITE ANALYSIS ---
        double prerequisiteProficiency = -1.0; // Default: No Data / Neutral

        if (quest.SubjectId.HasValue)
        {
            // 1. Find Skills taught by this Subject
            var subjectMappings = await _mappingRepository.GetMappingsBySubjectIdsAsync(new[] { quest.SubjectId.Value }, cancellationToken);
            var subjectSkillIds = subjectMappings.Select(m => m.SkillId).ToList();

            if (subjectSkillIds.Any())
            {
                // 2. Find Prerequisites for those skills
                var allDependencies = await _skillDependencyRepository.GetAllAsync(cancellationToken);
                var prerequisites = allDependencies
                    .Where(d => subjectSkillIds.Contains(d.SkillId)) // Where target is this subject's skill
                    .Select(d => d.PrerequisiteSkillId)
                    .Distinct()
                    .ToList();

                if (prerequisites.Any())
                {
                    // 3. Check User's Level in Prerequisites
                    var userSkills = await _userSkillRepository.GetSkillsByAuthIdAsync(request.AuthUserId, cancellationToken);
                    var userSkillMap = userSkills.ToDictionary(s => s.SkillId);

                    int totalPrereqs = prerequisites.Count;
                    int metPrereqs = 0;
                    int unknownPrereqs = 0;

                    foreach (var prereqId in prerequisites)
                    {
                        if (userSkillMap.TryGetValue(prereqId, out var us))
                        {
                            // REFACTOR: Check Level >= 2. Match GenerateQuestLine logic.
                            if (us.Level >= 2) metPrereqs++;
                        }
                        else
                        {
                            unknownPrereqs++;
                        }
                    }

                    if (unknownPrereqs == totalPrereqs)
                    {
                        prerequisiteProficiency = 1.0; // Assume standard if no data
                    }
                    else
                    {
                        // Treat unknowns as neutral/met
                        prerequisiteProficiency = (double)(metPrereqs + unknownPrereqs) / totalPrereqs;
                    }

                    _logger.LogInformation("Prerequisite Proficiency: {Percent:P0} (Met: {Met}, Unknown: {Unknown}, Total: {Total})",
                        prerequisiteProficiency, metPrereqs, unknownPrereqs, totalPrereqs);
                }
            }
        }
        // -------------------------------------

        string calculatedDifficulty = "Standard";
        if (quest.SubjectId.HasValue)
        {
            var gradeRecords = await _studentSubjectRepository.GetSemesterSubjectsByUserAsync(request.AuthUserId, cancellationToken);
            var subjectRecord = gradeRecords.FirstOrDefault(s => s.SubjectId == quest.SubjectId.Value);

            // Pass the proficiency to the resolver
            var difficultyInfo = _difficultyResolver.ResolveDifficulty(subjectRecord, prerequisiteProficiency);
            calculatedDifficulty = difficultyInfo.ExpectedDifficulty;
        }
        else if (!string.IsNullOrEmpty(quest.ExpectedDifficulty))
        {
            calculatedDifficulty = quest.ExpectedDifficulty;
        }

        if (existingAttempt != null)
        {
            // If already active, respect history unless it was just a preview ("NotStarted")
            if (existingAttempt.Status != QuestAttemptStatus.NotStarted)
            {
                return new StartQuestResponse
                {
                    AttemptId = existingAttempt.Id,
                    Status = existingAttempt.Status.ToString(),
                    AssignedDifficulty = existingAttempt.AssignedDifficulty,
                    IsNew = false
                };
            }

            // Activating for the first time -> Lock calculated difficulty
            existingAttempt.Status = QuestAttemptStatus.InProgress;
            existingAttempt.AssignedDifficulty = calculatedDifficulty;
            existingAttempt.Notes = $"Started. Difficulty: {calculatedDifficulty} (Prereq Prof: {prerequisiteProficiency:P0})";
            existingAttempt.StartedAt = DateTimeOffset.UtcNow;
            existingAttempt.UpdatedAt = DateTimeOffset.UtcNow;

            await _attemptRepository.UpdateAsync(existingAttempt, cancellationToken);

            return new StartQuestResponse
            {
                AttemptId = existingAttempt.Id,
                Status = existingAttempt.Status.ToString(),
                AssignedDifficulty = calculatedDifficulty,
                IsNew = true
            };
        }

        var newAttempt = new UserQuestAttempt
        {
            Id = Guid.NewGuid(),
            AuthUserId = request.AuthUserId,
            QuestId = request.QuestId,
            Status = QuestAttemptStatus.InProgress,
            AssignedDifficulty = calculatedDifficulty,
            Notes = $"First attempt. Prereq Prof: {prerequisiteProficiency:P0}",
            StartedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var createdAttempt = await _attemptRepository.AddAsync(newAttempt, cancellationToken);

        return new StartQuestResponse
        {
            AttemptId = createdAttempt.Id,
            Status = createdAttempt.Status.ToString(),
            AssignedDifficulty = calculatedDifficulty,
            IsNew = true
        };
    }
}