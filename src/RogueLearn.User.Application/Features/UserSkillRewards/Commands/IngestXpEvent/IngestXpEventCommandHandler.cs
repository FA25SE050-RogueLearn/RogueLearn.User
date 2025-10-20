using MediatR;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;

public class IngestXpEventCommandHandler : IRequestHandler<IngestXpEventCommand, IngestXpEventResponse>
{
    private readonly IUserSkillRewardRepository _userSkillRewardRepository;
    private readonly IUserSkillRepository _userSkillRepository;

    public IngestXpEventCommandHandler(
        IUserSkillRewardRepository userSkillRewardRepository,
        IUserSkillRepository userSkillRepository)
    {
        _userSkillRewardRepository = userSkillRewardRepository;
        _userSkillRepository = userSkillRepository;
    }

    public async Task<IngestXpEventResponse> Handle(IngestXpEventCommand request, CancellationToken cancellationToken)
    {
        // Basic idempotency check: skip if a reward with same source exists for this user
        if (!string.IsNullOrWhiteSpace(request.SourceService) && request.SourceId.HasValue)
        {
            var existingReward = await _userSkillRewardRepository.FirstOrDefaultAsync(
                r => r.AuthUserId == request.AuthUserId && r.SourceService == request.SourceService && r.SourceId == request.SourceId,
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

        // Persist reward entry
        var reward = new UserSkillReward
        {
            AuthUserId = request.AuthUserId,
            SourceService = request.SourceService,
            SourceType = request.SourceType,
            SourceId = request.SourceId,
            SkillName = request.SkillName,
            PointsAwarded = request.Points,
            Reason = request.Reason,
            CreatedAt = request.OccurredAt ?? DateTimeOffset.UtcNow
        };
        await _userSkillRewardRepository.AddAsync(reward, cancellationToken);

        // Update or create the user's skill record
        var userSkill = await _userSkillRepository.FirstOrDefaultAsync(
            s => s.AuthUserId == request.AuthUserId && s.SkillName == request.SkillName,
            cancellationToken);
        if (userSkill is null)
        {
            userSkill = new UserSkill
            {
                AuthUserId = request.AuthUserId,
                SkillName = request.SkillName,
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

    // Minimal leveling function; can be replaced by a more sophisticated progression curve later.
    private static int CalculateLevel(int experiencePoints)
    {
        // Level 1 at 0 XP, then +1 level every 1000 XP
        var baseLevel = 1;
        var additional = experiencePoints / 1000;
        return Math.Max(baseLevel, baseLevel + additional);
    }
}