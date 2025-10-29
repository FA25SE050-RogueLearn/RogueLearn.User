using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IUserQuestProgressRepository : IGenericRepository<UserQuestProgress>
{
    // NEW METHOD: A specialized method to efficiently fetch progress for a user and a specific list of quests.
    Task<IEnumerable<UserQuestProgress>> GetUserProgressForQuestsAsync(Guid authUserId, List<Guid> questIds, CancellationToken cancellationToken = default);
}
