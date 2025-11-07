using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IUserSkillRewardRepository : IGenericRepository<UserSkillReward>
{
    // A new specialized method to handle the complex query for the idempotency check.
    Task<UserSkillReward?> GetBySourceAsync(Guid authUserId, string sourceService, Guid sourceId, CancellationToken cancellationToken = default);
}
