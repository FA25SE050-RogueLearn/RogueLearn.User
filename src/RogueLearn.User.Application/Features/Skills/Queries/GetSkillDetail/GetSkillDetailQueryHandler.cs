using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Skills.Queries.GetSkillDetail;

public class GetSkillDetailQueryHandler : IRequestHandler<GetSkillDetailQuery, SkillDetailDto?>
{
    private const int SKILL_MASTERY_LEVEL_THRESHOLD = 5; // Level required to consider a skill "Complete"

    private readonly ISkillRepository _skillRepository;
    private readonly IUserSkillRepository _userSkillRepository;
    private readonly ISkillDependencyRepository _dependencyRepository;
    private readonly ISubjectSkillMappingRepository _mappingRepository;
    private readonly IQuestRepository _questRepository;
    private readonly ISubjectRepository _subjectRepository; // Needed to link mappings to quests via subject
    private readonly ILogger<GetSkillDetailQueryHandler> _logger;

    public GetSkillDetailQueryHandler(
        ISkillRepository skillRepository,
        IUserSkillRepository userSkillRepository,
        ISkillDependencyRepository dependencyRepository,
        ISubjectSkillMappingRepository mappingRepository,
        IQuestRepository questRepository,
        ISubjectRepository subjectRepository,
        ILogger<GetSkillDetailQueryHandler> logger)
    {
        _skillRepository = skillRepository;
        _userSkillRepository = userSkillRepository;
        _dependencyRepository = dependencyRepository;
        _mappingRepository = mappingRepository;
        _questRepository = questRepository;
        _subjectRepository = subjectRepository;
        _logger = logger;
    }

    public async Task<SkillDetailDto?> Handle(GetSkillDetailQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching skill details for SkillId={SkillId}, UserId={UserId}", request.SkillId, request.AuthUserId);

        // 1. Get Master Skill Data
        var skill = await _skillRepository.GetByIdAsync(request.SkillId, cancellationToken);
        if (skill == null) return null;

        // 2. Get User Progress for THIS skill
        var userSkills = await _userSkillRepository.GetSkillsByAuthIdAsync(request.AuthUserId, cancellationToken);
        var userSkillMap = userSkills.ToDictionary(us => us.SkillId);
        var currentUserSkill = userSkillMap.GetValueOrDefault(request.SkillId);

        // Calculate Progress (Logic: Level N = (N-1)*1000 XP. Next level needs 1000 XP in current tier)
        // Assuming 1000 XP per level flat scaling for UI simplicity based on your mock
        int currentXpTotal = currentUserSkill?.ExperiencePoints ?? 0;
        int level = currentUserSkill?.Level ?? 0;
        int xpForNextLevel = 1000;
        int xpInCurrentLevel = currentXpTotal % 1000;
        double progressPerc = Math.Round((double)xpInCurrentLevel / xpForNextLevel * 100, 1);

        // 3. Dependencies (Prerequisites & Unlocks)
        var allDependencies = await _dependencyRepository.GetAllAsync(cancellationToken);

        // Prerequisites: Skills that point TO this skill
        var prerequisites = allDependencies.Where(d => d.SkillId == request.SkillId).ToList();

        // Unlocks: Skills that this skill points TO (as a prerequisite)
        var unlocks = allDependencies.Where(d => d.PrerequisiteSkillId == request.SkillId).ToList();

        // Fetch names for dependencies
        var allSkillIds = prerequisites.Select(p => p.PrerequisiteSkillId)
                          .Concat(unlocks.Select(u => u.SkillId))
                          .Distinct().ToList();

        var relatedSkills = (await _skillRepository.GetAllAsync(cancellationToken))
                            .Where(s => allSkillIds.Contains(s.Id))
                            .ToDictionary(s => s.Id);

        // 4. Learning Path (Quests)
        // Strategy: Skill -> SubjectSkillMapping -> Subject -> Quests linked to Subject
        var mappings = (await _mappingRepository.GetAllAsync(cancellationToken))
                       .Where(m => m.SkillId == request.SkillId)
                       .Select(m => m.SubjectId)
                       .ToList();

        var allQuests = await _questRepository.GetAllAsync(cancellationToken);
        var linkedQuests = allQuests
            .Where(q => q.SubjectId.HasValue && mappings.Contains(q.SubjectId.Value) && q.IsActive)
            .ToList();

        // --- Build DTO ---
        var dto = new SkillDetailDto
        {
            Id = skill.Id,
            Name = skill.Name,
            Domain = skill.Domain ?? "General",
            Tier = skill.Tier.ToString(),
            Description = skill.Description ?? "No description available.",
            CurrentLevel = level,
            CurrentXp = currentXpTotal,
            XpForNextLevel = xpForNextLevel,
            XpProgressInLevel = xpInCurrentLevel,
            ProgressPercentage = progressPerc
        };

        // Map Prerequisites
        foreach (var dep in prerequisites)
        {
            if (relatedSkills.TryGetValue(dep.PrerequisiteSkillId, out var prereqSkill))
            {
                var prereqUserSkill = userSkillMap.GetValueOrDefault(dep.PrerequisiteSkillId);
                var prereqLevel = prereqUserSkill?.Level ?? 0;

                var isMet = prereqLevel >= SKILL_MASTERY_LEVEL_THRESHOLD;

                var status = isMet
                    ? "100% Complete (Mastered)"
                    : $"{prereqLevel}/{SKILL_MASTERY_LEVEL_THRESHOLD} Levels";

                dto.Prerequisites.Add(new DependencyStatusDto
                {
                    SkillId = prereqSkill.Id,
                    Name = prereqSkill.Name,
                    IsMet = isMet,
                    UserLevel = prereqLevel,
                    StatusLabel = status
                });
            }
        }

        // Map Unlocks
        foreach (var dep in unlocks)
        {
            if (relatedSkills.TryGetValue(dep.SkillId, out var unlockSkill))
            {
                var isAvailable = level >= SKILL_MASTERY_LEVEL_THRESHOLD;

                dto.Unlocks.Add(new DependencyStatusDto
                {
                    SkillId = unlockSkill.Id,
                    Name = unlockSkill.Name,
                    IsMet = isAvailable, // "IsMet" in UI context usually means "Is Available/Unlocked"
                    StatusLabel = isAvailable ? "Available" : $"Requires {skill.Name} Mastery (Lv.{SKILL_MASTERY_LEVEL_THRESHOLD})"
                });
            }
        }

        // Map Learning Path
        foreach (var q in linkedQuests)
        {
            dto.LearningPath.Add(new SkillQuestDto
            {
                QuestId = q.Id,
                Title = q.Title,
                XpReward = q.ExperiencePointsReward,
                Type = "Quest"
            });
        }

        return dto;
    }
}