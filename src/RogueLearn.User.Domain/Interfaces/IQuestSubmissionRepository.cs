using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IQuestSubmissionRepository : IGenericRepository<QuestSubmission>
{
    Task<List<QuestSubmission>> GetByActivityIdAsync(Guid activityId, CancellationToken cancellationToken = default);
    Task<QuestSubmission?> GetLatestByActivityAndUserAsync(Guid activityId, Guid userId, CancellationToken cancellationToken = default);
}
