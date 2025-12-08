// RogueLearn.User/src/RogueLearn.User.Domain/Interfaces/IQuestStepRepository.cs
using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

/// <summary>
/// Defines the contract for a repository that manages QuestStep entities.
/// Inherits generic repository operations from IGenericRepository.
/// </summary>
public interface IQuestStepRepository : IGenericRepository<QuestStep>
{
    // MODIFICATION START: Added a new, specialized method to reliably find steps by quest ID.
    // This is necessary to work around limitations in the generic FindAsync LINQ provider.
    /// <summary>
    /// Fetches all quest steps associated with a specific quest ID.
    /// </summary>
    /// <param name="questId">The ID of the quest.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of quest steps for the given quest.</returns>
    Task<IEnumerable<QuestStep>> GetByQuestIdAsync(Guid questId, CancellationToken cancellationToken = default);
    Task<bool> QuestContainsSteps(Guid questId, CancellationToken cancellationToken = default);
    // MODIFICATION END
}