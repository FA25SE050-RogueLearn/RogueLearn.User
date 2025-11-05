// RogueLearn.User/src/RogueLearn.User.Application/Features/UserSkillRewards/Commands/IngestXpEvent/IngestXpEventCommandHandler.cs
using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;

public class IngestXpEventCommandHandler : IRequestHandler<IngestXpEventCommand, IngestXpEventResponse>
{
    private readonly IUserSkillRewardRepository _userSkillRewardRepository;
    private readonly IUserSkillRepository _userSkillRepository;
    private readonly ISkillRepository _skillRepository;

    public IngestXpEventCommandHandler(
        IUserSkillRewardRepository userSkillRewardRepository,
        IUserSkillRepository userSkillRepository,
        ISkillRepository skillRepository)
    {
        _userSkillRewardRepository = userSkillRewardRepository;
        _userSkillRepository = userSkillRepository;
        _skillRepository = skillRepository;
    }

    public async Task<IngestXpEventResponse> Handle(IngestXpEventCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.SourceService) && request.SourceId.HasValue)
        {
            var existingReward = await _userSkillRewardRepository.GetBySourceAsync(
                request.AuthUserId,
                request.SourceService,
                request.SourceId.Value,
                cancellationToken);

            if (existingReward is not null)
            {
                return new IngestXpEventResponse
                {
                    Processed = false,
                    RewardId = existingReward.Id,
                    Message = "XP event already processed",
                    SkillName = existingReward.SkillName,
                    NewExperiencePoints = 0,
                    NewLevel = 0
                };
            }
        }

        var parsedSourceType = SkillRewardSourceType.QuestComplete;
        if (!string.IsNullOrWhiteSpace(request.SourceType))
        {
            if (Enum.TryParse<SkillRewardSourceType>(request.SourceType, ignoreCase: true, out var st))
            {
                parsedSourceType = st;
            }
        }

        if (!request.SkillId.HasValue)
        {
            throw new RogueLearn.User.Application.Exceptions.BadRequestException("SkillId is required to ingest an XP event.");
        }

        // MODIFICATION: The skill is now fetched from the database using the reliable SkillId.
        var skill = await _skillRepository.GetByIdAsync(request.SkillId.Value, cancellationToken);
        if (skill is null)
        {
            throw new RogueLearn.User.Application.Exceptions.BadRequestException($"Unknown skill with ID '{request.SkillId.Value}'. Ensure the skill exists in the catalog.");
        }

        var reward = new UserSkillReward
        {
            AuthUserId = request.AuthUserId,
            SourceService = request.SourceService,
            SourceType = parsedSourceType,
            SourceId = request.SourceId,
            SkillName = skill.Name, // We get the name from the skill object we just fetched.
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
                SkillName = skill.Name,
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