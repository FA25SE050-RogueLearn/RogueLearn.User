using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IUserQuestStepProgressRepository : IGenericRepository<UserQuestStepProgress>
{
    Task<int> GetCompletedStepsCountForAttemptAsync(Guid attemptId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserQuestStepProgress>> GetByAttemptIdAsync(Guid attemptId, CancellationToken cancellationToken = default);
    Task DeleteByAttemptIdAsync(Guid attemptId, CancellationToken cancellationToken = default);
}