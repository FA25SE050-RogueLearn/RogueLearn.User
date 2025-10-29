using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.UserSkillRewards.Queries.GetUserSkillRewards;

public sealed class GetUserSkillRewardsQueryHandler : IRequestHandler<GetUserSkillRewardsQuery, GetUserSkillRewardsResponse>
{
    private readonly IUserSkillRewardRepository _repository;

    public GetUserSkillRewardsQueryHandler(IUserSkillRewardRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetUserSkillRewardsResponse> Handle(GetUserSkillRewardsQuery request, CancellationToken cancellationToken)
    {
        var rewards = await _repository.FindAsync(r => r.AuthUserId == request.UserId, cancellationToken);
            return new GetUserSkillRewardsResponse
            {
                Rewards = rewards.Select(r => new UserSkillRewardDto
                {
                    Id = r.Id,
                    SourceService = r.SourceService,
                    // Map enum to string for DTO
                    SourceType = r.SourceType.ToString(),
                    SourceId = r.SourceId,
                    SkillName = r.SkillName,
                    PointsAwarded = r.PointsAwarded,
                    Reason = r.Reason,
                    CreatedAt = r.CreatedAt
                }).ToList()
            };
    }
}