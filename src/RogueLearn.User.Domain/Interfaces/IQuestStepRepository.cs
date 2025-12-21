using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IQuestStepRepository : IGenericRepository<QuestStep>
{
    Task<IEnumerable<QuestStep>> GetByQuestIdAsync(Guid questId, CancellationToken cancellationToken = default);
    Task<bool> QuestContainsSteps(Guid questId, CancellationToken cancellationToken = default);
}