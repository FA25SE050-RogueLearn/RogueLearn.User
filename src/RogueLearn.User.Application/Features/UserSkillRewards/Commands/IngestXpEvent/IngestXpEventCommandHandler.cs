using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;

public class IngestXpEventCommandHandler : IRequestHandler<IngestXpEventCommand, IngestXpEventResponse>
{
    private readonly IUserSkillRewardRepository _userSkillRewardRepository;
    private readonly IUserSkillRepository _userSkillRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ILogger<IngestXpEventCommandHandler> _logger;

    public IngestXpEventCommandHandler(
        IUserSkillRewardRepository userSkillRewardRepository,
        IUserSkillRepository userSkillRepository,
        ISkillRepository skillRepository,
        ILogger<IngestXpEventCommandHandler> logger)
    {
        _userSkillRewardRepository = userSkillRewardRepository;
        _userSkillRepository = userSkillRepository;
        _skillRepository = skillRepository;
        _logger = logger;
    }

    public async Task<IngestXpEventResponse> Handle(IngestXpEventCommand request, CancellationToken cancellationToken)
    {
        // 1. Parse the input string into the Enum
        var parsedSourceEnum = SkillRewardSourceType.QuestComplete; // Default
        if (!string.IsNullOrWhiteSpace(request.SourceService))
        {
            // Try parsing the input string (e.g. "QuestSystem" or "ActivityComplete") to the enum
            if (Enum.TryParse<SkillRewardSourceType>(request.SourceService, ignoreCase: true, out var st))
            {
                parsedSourceEnum = st;
            }
            // If the input was "QuestSystem" (old service name) but not in enum, we default to QuestComplete or similar.
            // You might want to map specific old strings to new Enum values here if needed.
        }

        // 2. Idempotency check using the Enum and SourceId
        if (request.SourceId.HasValue && request.SkillId.HasValue)
        {
            var existingReward = await _userSkillRewardRepository.GetBySourceAndSkillAsync(
                request.AuthUserId,
                parsedSourceEnum, // Use the Enum here
                request.SourceId.Value,
                request.SkillId.Value,
                cancellationToken);

            if (existingReward is not null)
            {
                // We fetch the skill name for the response message only (not stored in reward table anymore)
                var existingSkill = await _skillRepository.GetByIdAsync(existingReward.SkillId, cancellationToken);

                return new IngestXpEventResponse
                {
                    Processed = false,
                    RewardId = existingReward.Id,
                    Message = "XP event already processed for this skill",
                    SkillName = existingSkill?.Name ?? "Unknown Skill",
                    NewExperiencePoints = 0,
                    NewLevel = 0
                };
            }
        }

        if (!request.SkillId.HasValue)
        {
            throw new RogueLearn.User.Application.Exceptions.BadRequestException("SkillId is required to ingest an XP event.");
        }

        var skill = await _skillRepository.GetByIdAsync(request.SkillId.Value, cancellationToken);
        if (skill is null)
        {
            throw new RogueLearn.User.Application.Exceptions.BadRequestException($"Unknown skill with ID '{request.SkillId.Value}'.");
        }

        var reward = new UserSkillReward
        {
            AuthUserId = request.AuthUserId,
            SourceService = parsedSourceEnum, // Storing Enum
            SourceId = request.SourceId,      // Keeping SourceId
            SkillId = skill.Id,
            PointsAwarded = request.Points,
            Reason = request.Reason,
            CreatedAt = request.OccurredAt ?? DateTimeOffset.UtcNow
        };
        await _userSkillRewardRepository.AddAsync(reward, cancellationToken);

        var userSkill = await _userSkillRepository.FirstOrDefaultAsync(
            s => s.AuthUserId == request.AuthUserId && s.SkillId == request.SkillId.Value,
            cancellationToken);

        if (userSkill is null)
        {
            userSkill = new UserSkill
            {
                AuthUserId = request.AuthUserId,
                SkillId = skill.Id,
                SkillName = skill.Name, // Redundant denormalization kept in UserSkill per schema
                ExperiencePoints = Math.Max(0, request.Points),
                Level = CalculateLevel(Math.Max(0, request.Points)),
                LastUpdatedAt = DateTimeOffset.UtcNow
            };
            await _userSkillRepository.AddAsync(userSkill, cancellationToken);
        }
        else
        {
            userSkill.ExperiencePoints = Math.Max(0, userSkill.ExperiencePoints + request.Points);
            userSkill.Level = CalculateLevel(userSkill.ExperiencePoints);
            userSkill.LastUpdatedAt = DateTimeOffset.UtcNow;
            await _userSkillRepository.UpdateAsync(userSkill, cancellationToken);
        }

        return new IngestXpEventResponse
        {
            Processed = true,
            RewardId = reward.Id,
            Message = "XP event ingested",
            SkillName = userSkill.SkillName,
            NewExperiencePoints = userSkill.ExperiencePoints,
            NewLevel = userSkill.Level
        };
    }

    private static int CalculateLevel(int experiencePoints)
    {
        var baseLevel = 1;
        var additional = experiencePoints / 1000;
        return Math.Max(baseLevel, baseLevel + additional);
    }
}