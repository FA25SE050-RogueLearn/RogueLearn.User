using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IUserSkillRewardRepository : IGenericRepository<UserSkillReward>
{
    // A new specialized method to handle the complex query for the idempotency check.
    Task<UserSkillReward?> GetBySourceAsync(Guid authUserId, string sourceService, Guid sourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a UserSkillReward by source AND skill ID.
    /// This ensures proper idempotency when a single subject maps to multiple skills.
    /// </summary>
    Task<UserSkillReward?> GetBySourceAndSkillAsync(Guid authUserId, string sourceService, Guid sourceId, Guid skillId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all rewards for a given user and source (e.g., BossFight match result).
    /// </summary>
    Task<IEnumerable<UserSkillReward>> GetBySourceAllAsync(Guid authUserId, string sourceService, Guid sourceId, CancellationToken cancellationToken = default);
}
