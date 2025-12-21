using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IUserSkillRewardRepository : IGenericRepository<UserSkillReward>
{
    Task<UserSkillReward?> GetBySourceAsync(Guid authUserId, string sourceService, Guid sourceId, CancellationToken cancellationToken = default);

    Task<UserSkillReward?> GetBySourceAndSkillAsync(Guid authUserId, string sourceService, Guid sourceId, Guid skillId, CancellationToken cancellationToken = default);

    Task<IEnumerable<UserSkillReward>> GetBySourceAllAsync(Guid authUserId, string sourceService, Guid sourceId, CancellationToken cancellationToken = default);
}
