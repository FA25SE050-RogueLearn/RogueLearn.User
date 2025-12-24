using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Domain.Interfaces;

public interface IUserSkillRewardRepository : IGenericRepository<UserSkillReward>
{
    // Updated to use Enum for sourceService
    Task<UserSkillReward?> GetBySourceAsync(Guid authUserId, SkillRewardSourceType sourceService, Guid sourceId, CancellationToken cancellationToken = default);

    Task<UserSkillReward?> GetBySourceAndSkillAsync(Guid authUserId, SkillRewardSourceType sourceService, Guid sourceId, Guid skillId, CancellationToken cancellationToken = default);

    Task<IEnumerable<UserSkillReward>> GetBySourceAllAsync(Guid authUserId, SkillRewardSourceType sourceService, Guid sourceId, CancellationToken cancellationToken = default);
}