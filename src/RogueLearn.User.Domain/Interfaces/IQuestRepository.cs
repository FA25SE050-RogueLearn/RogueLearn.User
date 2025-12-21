using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IQuestRepository : IGenericRepository<Quest>
{
    Task<IEnumerable<Quest>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<Quest?> GetActiveQuestBySubjectIdAsync(Guid subjectId, CancellationToken cancellationToken = default);
}