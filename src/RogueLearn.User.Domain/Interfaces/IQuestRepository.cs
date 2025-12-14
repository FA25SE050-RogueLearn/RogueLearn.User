// src/RogueLearn.User.Domain/Interfaces/IQuestRepository.cs
using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IQuestRepository : IGenericRepository<Quest>
{
    // REMOVED: GetQuestsByChapterIdsAsync

    Task<IEnumerable<Quest>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    // ADDED: specialized method for safe filtering by subject_id
    Task<Quest?> GetActiveQuestBySubjectIdAsync(Guid subjectId, CancellationToken cancellationToken = default);
}