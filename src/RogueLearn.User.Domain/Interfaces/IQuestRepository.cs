// src/RogueLearn.User.Domain/Interfaces/IQuestRepository.cs
using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IQuestRepository : IGenericRepository<Quest>
{
    Task<IEnumerable<Quest>> GetQuestsByChapterIdsAsync(IEnumerable<Guid> chapterIds, CancellationToken cancellationToken = default);
    Task<IEnumerable<Quest>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}