namespace RogueLearn.User.Domain.Interfaces;

public interface INoteTagRepository
{
    Task AddAsync(Guid noteId, Guid tagId, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid noteId, Guid tagId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Guid>> GetTagIdsForNoteAsync(Guid noteId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Guid>> GetNoteIdsByTagIdAsync(Guid tagId, CancellationToken cancellationToken = default);
}