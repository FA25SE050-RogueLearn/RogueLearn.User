// RogueLearn.User/src/RogueLearn.User.Domain/Interfaces/IUserQuestStepProgressRepository.cs
using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IUserQuestStepProgressRepository : IGenericRepository<UserQuestStepProgress>
{
    // ADDED: A new, specialized method to reliably count completed steps for an attempt.
    // This will bypass the faulty LINQ provider by using explicit filters in the implementation.
    Task<int> GetCompletedStepsCountForAttemptAsync(Guid attemptId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserQuestStepProgress>> GetByAttemptIdAsync(Guid attemptId, CancellationToken cancellationToken = default);
}